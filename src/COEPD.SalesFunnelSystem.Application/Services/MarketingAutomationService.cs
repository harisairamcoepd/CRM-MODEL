using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class MarketingAutomationService : IMarketingAutomationService
{
    private readonly IEnumerable<ILeadCreatedTrigger> _triggers;
    private readonly ILogger<MarketingAutomationService> _logger;

    public MarketingAutomationService(
        IEnumerable<ILeadCreatedTrigger> triggers,
        ILogger<MarketingAutomationService> logger)
    {
        _triggers = triggers;
        _logger = logger;
    }

    public async Task TriggerLeadCreatedAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        foreach (var trigger in _triggers)
        {
            try
            {
                await trigger.ExecuteAsync(lead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lead created trigger {TriggerName} failed for lead {LeadId}.", trigger.GetType().Name, lead.Id);
            }
        }
    }
}

public class FollowUpScheduler : IFollowUpScheduler
{
    private readonly ILeadFollowUpJobRepository _leadFollowUpJobRepository;

    public FollowUpScheduler(ILeadFollowUpJobRepository leadFollowUpJobRepository)
    {
        _leadFollowUpJobRepository = leadFollowUpJobRepository;
    }

    public async Task ScheduleLeadFollowUpsAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _leadFollowUpJobRepository.AddAsync(new LeadFollowUpJob
        {
            LeadId = lead.Id,
            FollowUpType = FollowUpJobTypes.OneHour,
            DueAt = now.AddHours(1),
            Status = FollowUpJobStatuses.Pending
        }, cancellationToken);

        await _leadFollowUpJobRepository.AddAsync(new LeadFollowUpJob
        {
            LeadId = lead.Id,
            FollowUpType = FollowUpJobTypes.OneDay,
            DueAt = now.AddDays(1),
            Status = FollowUpJobStatuses.Pending
        }, cancellationToken);
    }
}

public class LeadCreatedActivityTrigger : ILeadCreatedTrigger
{
    private readonly ILeadActivityRepository _leadActivityRepository;

    public LeadCreatedActivityTrigger(ILeadActivityRepository leadActivityRepository)
    {
        _leadActivityRepository = leadActivityRepository;
    }

    public async Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _leadActivityRepository.AddAsync(new LeadActivityLog
        {
            LeadId = lead.Id,
            ActivityType = "LeadCreated",
            Message = "Lead saved successfully.",
            Status = "Success"
        }, cancellationToken);
    }
}

public class LeadCreatedEmailTrigger : ILeadCreatedTrigger
{
    private readonly IEmailAutomationService _emailAutomationService;
    private readonly ILeadActivityRepository _leadActivityRepository;
    private readonly ILogger<LeadCreatedEmailTrigger> _logger;

    public LeadCreatedEmailTrigger(
        IEmailAutomationService emailAutomationService,
        ILeadActivityRepository leadActivityRepository,
        ILogger<LeadCreatedEmailTrigger> logger)
    {
        _emailAutomationService = emailAutomationService;
        _leadActivityRepository = leadActivityRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        try
        {
            await _emailAutomationService.TriggerWelcomeSequenceAsync(lead, cancellationToken);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "EmailSent",
                Message = $"Welcome email automation completed for {lead.Email}.",
                Status = "Success"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email automation failed for lead {LeadId}.", lead.Id);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "EmailSent",
                Message = "SMTP email failed.",
                Status = "Failed"
            }, cancellationToken);
        }
    }
}

public class LeadCreatedWhatsAppTrigger : ILeadCreatedTrigger
{
    private readonly IWhatsAppAutomationService _whatsAppAutomationService;
    private readonly ILeadActivityRepository _leadActivityRepository;
    private readonly ILogger<LeadCreatedWhatsAppTrigger> _logger;

    public LeadCreatedWhatsAppTrigger(
        IWhatsAppAutomationService whatsAppAutomationService,
        ILeadActivityRepository leadActivityRepository,
        ILogger<LeadCreatedWhatsAppTrigger> logger)
    {
        _whatsAppAutomationService = whatsAppAutomationService;
        _leadActivityRepository = leadActivityRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        try
        {
            await _whatsAppAutomationService.SendLeadCapturedMessageAsync(lead, cancellationToken);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "WhatsAppSimulated",
                Message = $"WhatsApp simulated send for {lead.Phone}.",
                Status = "Success"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp automation failed for lead {LeadId}.", lead.Id);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "WhatsAppSimulated",
                Message = "WhatsApp simulation failed.",
                Status = "Failed"
            }, cancellationToken);
        }
    }
}

public class LeadCreatedFollowUpScheduleTrigger : ILeadCreatedTrigger
{
    private readonly IFollowUpScheduler _followUpScheduler;
    private readonly ILeadActivityRepository _leadActivityRepository;

    public LeadCreatedFollowUpScheduleTrigger(
        IFollowUpScheduler followUpScheduler,
        ILeadActivityRepository leadActivityRepository)
    {
        _followUpScheduler = followUpScheduler;
        _leadActivityRepository = leadActivityRepository;
    }

    public async Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _followUpScheduler.ScheduleLeadFollowUpsAsync(lead, cancellationToken);
        await _leadActivityRepository.AddAsync(new LeadActivityLog
        {
            LeadId = lead.Id,
            ActivityType = "FollowUpScheduled",
            Message = "Follow-ups scheduled for 1 hour and 1 day.",
            Status = "Success"
        }, cancellationToken);
    }
}

public class LeadCreatedStaffNotificationTrigger : ILeadCreatedTrigger
{
    private readonly IStaffNotificationService _staffNotificationService;
    private readonly ILeadActivityRepository _leadActivityRepository;
    private readonly ILogger<LeadCreatedStaffNotificationTrigger> _logger;

    public LeadCreatedStaffNotificationTrigger(
        IStaffNotificationService staffNotificationService,
        ILeadActivityRepository leadActivityRepository,
        ILogger<LeadCreatedStaffNotificationTrigger> logger)
    {
        _staffNotificationService = staffNotificationService;
        _leadActivityRepository = leadActivityRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipients = await _staffNotificationService.NotifyLeadCreatedAsync(lead, cancellationToken);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "StaffNotified",
                Message = $"Staff notified for new lead. Recipients: {recipients}.",
                Status = "Success"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Staff notification failed for lead {LeadId}.", lead.Id);
            await _leadActivityRepository.AddAsync(new LeadActivityLog
            {
                LeadId = lead.Id,
                ActivityType = "StaffNotified",
                Message = "Staff notification failed.",
                Status = "Failed"
            }, cancellationToken);
        }
    }
}
