using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace COEPD.SalesFunnelSystem.Infrastructure.Services;

public class LeadFollowUpProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeadFollowUpProcessor> _logger;

    public LeadFollowUpProcessor(IServiceScopeFactory scopeFactory, ILogger<LeadFollowUpProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lead follow-up worker cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessDueJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var leadRepository = scope.ServiceProvider.GetRequiredService<ILeadRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<ILeadFollowUpJobRepository>();
        var emailAutomationService = scope.ServiceProvider.GetRequiredService<IEmailAutomationService>();
        var whatsAppAutomationService = scope.ServiceProvider.GetRequiredService<IWhatsAppAutomationService>();
        var leadActivityRepository = scope.ServiceProvider.GetRequiredService<ILeadActivityRepository>();

        var dueJobs = await jobRepository.GetDuePendingAsync(DateTime.UtcNow, 25, cancellationToken);
        foreach (var job in dueJobs)
        {
            var lead = await leadRepository.GetByIdAsync(job.LeadId, cancellationToken);
            if (lead is null)
            {
                job.Status = FollowUpJobStatuses.Failed;
                job.AttemptCount += 1;
                job.ProcessedAt = DateTime.UtcNow;
                await jobRepository.UpdateAsync(job, cancellationToken);
                continue;
            }

            try
            {
                await SendFollowUpAsync(job, lead, emailAutomationService, whatsAppAutomationService, cancellationToken);
                await leadActivityRepository.AddAsync(new LeadActivityLog
                {
                    LeadId = lead.Id,
                    ActivityType = "FollowUpSent",
                    Message = $"Follow-up sent ({job.FollowUpType}).",
                    Status = "Success"
                }, cancellationToken);

                job.Status = FollowUpJobStatuses.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process follow-up job {JobId} for lead {LeadId}.", job.Id, job.LeadId);
                await leadActivityRepository.AddAsync(new LeadActivityLog
                {
                    LeadId = lead.Id,
                    ActivityType = "FollowUpSent",
                    Message = $"Follow-up failed ({job.FollowUpType}).",
                    Status = "Failed"
                }, cancellationToken);

                job.Status = FollowUpJobStatuses.Failed;
            }

            job.AttemptCount += 1;
            job.ProcessedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job, cancellationToken);
        }
    }

    private static async Task SendFollowUpAsync(
        LeadFollowUpJob job,
        Lead lead,
        IEmailAutomationService emailAutomationService,
        IWhatsAppAutomationService whatsAppAutomationService,
        CancellationToken cancellationToken)
    {
        await emailAutomationService.TriggerLeadFollowUpAsync(lead, job.FollowUpType, cancellationToken);
        await whatsAppAutomationService.SendLeadFollowUpAsync(lead, job.FollowUpType, cancellationToken);
    }
}
