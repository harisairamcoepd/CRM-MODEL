using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Application.Common;
using COEPD.SalesFunnelSystem.Domain.Entities;
using COEPD.SalesFunnelSystem.Domain.Enums;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IFunnelTrackingService _funnelTrackingService;
    private readonly ILeadActivityRepository _leadActivityRepository;
    private readonly IMarketingAutomationService _marketingAutomationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IUserRepository _userRepository;
    private readonly IStaffNotificationService _staffNotificationService;

    public LeadService(
        ILeadRepository leadRepository,
        IFunnelTrackingService funnelTrackingService,
        ILeadActivityRepository leadActivityRepository,
        IMarketingAutomationService marketingAutomationService,
        IAnalyticsService analyticsService,
        IUserRepository userRepository,
        IStaffNotificationService staffNotificationService)
    {
        _leadRepository = leadRepository;
        _funnelTrackingService = funnelTrackingService;
        _leadActivityRepository = leadActivityRepository;
        _marketingAutomationService = marketingAutomationService;
        _analyticsService = analyticsService;
        _userRepository = userRepository;
        _staffNotificationService = staffNotificationService;
    }

    public async Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        var leadExists = await _leadRepository.ExistsByEmailOrPhoneAsync(email, phone, cancellationToken);
        if (leadExists)
        {
            throw new ConflictException("A lead with the same email or phone already exists.");
        }

        var lead = new Lead
        {
            Name = request.Name.Trim(),
            Phone = phone,
            Email = email,
            Location = request.Location.Trim(),
            Domain = request.Domain.Trim(),
            Source = NormalizeSource(request.Source),
            Status = string.IsNullOrWhiteSpace(request.Status) ? LeadStatuses.New : NormalizeStatus(request.Status),
            Score = string.IsNullOrWhiteSpace(request.Score) ? LeadScores.Warm : request.Score.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            FunnelStage = FunnelStages.New,
            AssignedStaffId = await ResolveAssignedStaffIdAsync(cancellationToken)
        };

        var created = await _leadRepository.AddAsync(lead, cancellationToken);
        var assignedStaffName = created.AssignedStaffId.HasValue
            ? (await _userRepository.GetByIdAsync(created.AssignedStaffId.Value, cancellationToken))?.FullName
            : null;
        await _funnelTrackingService.TrackAsync(created.Id, FunnelEventStages.Interest, cancellationToken);
        await _marketingAutomationService.TriggerLeadCreatedAsync(created, cancellationToken);
        _analyticsService.InvalidateDashboardCache();

        return new LeadResponse
        {
            Id = created.Id,
            Name = created.Name,
            Phone = created.Phone,
            Email = created.Email,
            Location = created.Location,
            Domain = created.Domain,
            Source = created.Source,
            Status = created.Status,
            Score = created.Score,
            Notes = created.Notes,
            FunnelStage = created.FunnelStage,
            AssignedStaffId = created.AssignedStaffId,
            AssignedStaffName = assignedStaffName,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<List<LeadResponse>> GetAllAsync(LeadFilterRequest filter, CancellationToken cancellationToken = default) =>
        MapLeadResponses(await _leadRepository.GetFilteredAsync(
            filter.Search,
            filter.Source,
            filter.Status,
            filter.Domain,
            filter.FromDate,
            filter.ToDate,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken));

    public async Task<List<LeadResponse>> GetTodayAsync(CancellationToken cancellationToken = default) =>
        MapLeadResponses(await _leadRepository.GetTodayAsync(cancellationToken));

    public async Task<List<LeadResponse>> GetAssignedAsync(int staffId, CancellationToken cancellationToken = default)
    {
        if (staffId <= 0)
        {
            throw new ArgumentException("Staff id must be greater than zero.");
        }

        return MapLeadResponses(await _leadRepository.GetAssignedToStaffAsync(staffId, cancellationToken));
    }

    public async Task<LeadResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lead not found.");

        return MapLeadResponse(lead);
    }

    public async Task<PipelineBoardResponse> GetPipelineAsync(LeadFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var leads = await GetAllAsync(filter, cancellationToken);
        return new PipelineBoardResponse
        {
            Stages =
            [
                BuildStage(LeadStatuses.New, leads),
                BuildStage(LeadStatuses.Contacted, leads),
                BuildStage(LeadStatuses.DemoBooked, leads, LeadStatuses.Demo, LeadStatuses.BookedLegacy),
                BuildStage(LeadStatuses.Converted, leads)
            ]
        };
    }

    public Task<UpdateLeadStatusResponse> MoveToStageAsync(int id, MoveLeadStageRequest request, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(id, new UpdateLeadStatusRequest { Status = request.Stage }, cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lead not found.");
        await _leadRepository.DeleteAsync(lead, cancellationToken);
        _analyticsService.InvalidateDashboardCache();
    }

    public async Task<UpdateLeadStatusResponse> UpdateStatusAsync(int id, UpdateLeadStatusRequest request, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lead not found.");

        var normalizedStatus = NormalizeStatus(request.Status);
        lead.Status = normalizedStatus;
        lead.FunnelStage = MapStatusToFunnelStage(normalizedStatus);

        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _leadActivityRepository.AddAsync(new LeadActivityLog
        {
            LeadId = lead.Id,
            ActivityType = "LeadStatusUpdated",
            Message = $"Lead status changed to {lead.Status}.",
            Status = "Success"
        }, cancellationToken);
        _analyticsService.InvalidateDashboardCache();

        return new UpdateLeadStatusResponse
        {
            Id = lead.Id,
            Status = lead.Status,
            FunnelStage = lead.FunnelStage,
            AssignedStaffId = lead.AssignedStaffId
        };
    }

    public async Task<LeadAssignmentResponse> AssignLeadAsync(int leadId, int staffId, CancellationToken cancellationToken = default)
    {
        if (leadId <= 0)
        {
            throw new ArgumentException("Lead id must be greater than zero.");
        }

        if (staffId <= 0)
        {
            throw new ArgumentException("Staff id must be greater than zero.");
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken) ?? throw new NotFoundException("Lead not found.");
        var staff = await _userRepository.GetByIdAsync(staffId, cancellationToken) ?? throw new NotFoundException("Staff user not found.");

        if (!staff.IsActive || staff.Role != UserRole.Staff)
        {
            throw new ArgumentException("Lead can only be assigned to an active staff user.");
        }

        lead.AssignedStaffId = staff.Id;
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _staffNotificationService.NotifyStaffLeadAssignedAsync(lead, staff, cancellationToken);
        await _leadActivityRepository.AddAsync(new LeadActivityLog
        {
            LeadId = lead.Id,
            UserId = staff.Id,
            ActivityType = "LeadAssigned",
            Message = $"Lead assigned to {staff.FullName}.",
            Status = "Success"
        }, cancellationToken);
        _analyticsService.InvalidateDashboardCache();

        return new LeadAssignmentResponse
        {
            LeadId = lead.Id,
            AssignedStaffId = staff.Id,
            AssignedStaffName = staff.FullName
        };
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }

        var value = status.Trim();
        return value.ToLowerInvariant() switch
        {
            "new" => LeadStatuses.New,
            "contacted" => LeadStatuses.Contacted,
            "demobooked" => LeadStatuses.DemoBooked,
            "converted" => LeadStatuses.Converted,
            _ => throw new ArgumentException("Invalid status. Allowed values: New, Contacted, DemoBooked, Converted.")
        };
    }

    private static string MapStatusToFunnelStage(string status) =>
        status switch
        {
            LeadStatuses.New => FunnelStages.New,
            LeadStatuses.Contacted => FunnelStages.Contacted,
            LeadStatuses.DemoBooked => FunnelStages.DemoBooked,
            LeadStatuses.Converted => FunnelStages.Enrolled,
            _ => FunnelStages.New
        };

    private static string NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return LeadSources.Website;
        }

        return source.Trim().ToLowerInvariant() switch
        {
            "website" => LeadSources.Website,
            "chatbot" => LeadSources.Chatbot,
            "ads" => LeadSources.Ads,
            _ => LeadSources.Website
        };
    }

    private static PipelineStageBucket BuildStage(string stage, List<LeadResponse> leads, params string[] aliases)
    {
        var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { stage };
        foreach (var alias in aliases)
        {
            allowedStatuses.Add(alias);
        }

        var stageLeads = leads.Where(x => allowedStatuses.Contains(x.Status)).ToList();
        var (displayName, description, accent, order, progressPercent) = GetStageMetadata(stage);

        return new PipelineStageBucket
        {
            Stage = stage,
            DisplayName = displayName,
            Description = description,
            Accent = accent,
            Order = order,
            ProgressPercent = progressPercent,
            Count = stageLeads.Count,
            Leads = stageLeads
        };
    }

    private static (string DisplayName, string Description, string Accent, int Order, int ProgressPercent) GetStageMetadata(string stage) =>
        stage switch
        {
            LeadStatuses.New => ("New", "Fresh inbound leads ready for first touch.", "new", 1, 25),
            LeadStatuses.Contacted => ("Contacted", "Leads already engaged by a counselor.", "contacted", 2, 50),
            LeadStatuses.DemoBooked => ("Demo Booked", "Qualified leads committed to a demo slot.", "demo", 3, 75),
            LeadStatuses.Converted => ("Converted", "Deals successfully moved into enrollment.", "converted", 4, 100),
            _ => (stage, "Pipeline stage", "new", 99, 0)
        };

    private async Task<int?> ResolveAssignedStaffIdAsync(CancellationToken cancellationToken)
    {
        var staffUsers = (await _userRepository.GetAllAsync(cancellationToken))
            .Where(x => x.IsActive && x.Role == UserRole.Staff)
            .ToList();

        if (staffUsers.Count == 0)
        {
            return null;
        }

        var staffLoads = new List<(AppUser User, int Count)>();
        foreach (var staffUser in staffUsers)
        {
            var assignedCount = await _leadRepository.CountAssignedToStaffAsync(staffUser.Id, cancellationToken);
            staffLoads.Add((staffUser, assignedCount));
        }

        return staffLoads
            .OrderBy(x => x.Count)
            .ThenBy(x => x.User.FullName)
            .Select(x => (int?)x.User.Id)
            .FirstOrDefault();
    }

    private static List<LeadResponse> MapLeadResponses(List<Lead> leads) =>
        leads.Select(MapLeadResponse).ToList();

    private static LeadResponse MapLeadResponse(Lead lead) =>
        new()
        {
            Id = lead.Id,
            Name = lead.Name,
            Phone = lead.Phone,
            Email = lead.Email,
            Location = lead.Location,
            Domain = lead.Domain,
            Source = lead.Source,
            Status = lead.Status,
            Score = lead.Score,
            Notes = lead.Notes,
            FunnelStage = lead.FunnelStage,
            AssignedStaffId = lead.AssignedStaffId,
            AssignedStaffName = lead.AssignedStaff?.FullName,
            CreatedAt = lead.CreatedAt
        };
}
