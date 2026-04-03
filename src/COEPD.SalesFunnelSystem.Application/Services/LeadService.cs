using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Application.Common;
using COEPD.SalesFunnelSystem.Domain.Entities;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IFunnelTrackingService _funnelTrackingService;
    private readonly ILeadActivityRepository _leadActivityRepository;
    private readonly IMarketingAutomationService _marketingAutomationService;
    private readonly IAnalyticsService _analyticsService;

    public LeadService(
        ILeadRepository leadRepository,
        IFunnelTrackingService funnelTrackingService,
        ILeadActivityRepository leadActivityRepository,
        IMarketingAutomationService marketingAutomationService,
        IAnalyticsService analyticsService)
    {
        _leadRepository = leadRepository;
        _funnelTrackingService = funnelTrackingService;
        _leadActivityRepository = leadActivityRepository;
        _marketingAutomationService = marketingAutomationService;
        _analyticsService = analyticsService;
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
            FunnelStage = FunnelStages.New
        };

        var created = await _leadRepository.AddAsync(lead, cancellationToken);
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
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<List<LeadResponse>> GetAllAsync(LeadFilterRequest filter, CancellationToken cancellationToken = default) =>
        (await _leadRepository.GetFilteredAsync(
            filter.Search,
            filter.Source,
            filter.Status,
            filter.Domain,
            filter.FromDate,
            filter.ToDate,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken))
        .Select(x => new LeadResponse
        {
            Id = x.Id,
            Name = x.Name,
            Phone = x.Phone,
            Email = x.Email,
            Location = x.Location,
            Domain = x.Domain,
            Source = x.Source,
            Status = x.Status,
            Score = x.Score,
            Notes = x.Notes,
            FunnelStage = x.FunnelStage,
            CreatedAt = x.CreatedAt
        }).ToList();

    public async Task<LeadResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Lead not found.");

        return new LeadResponse
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
            CreatedAt = lead.CreatedAt
        };
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
            FunnelStage = lead.FunnelStage
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
}
