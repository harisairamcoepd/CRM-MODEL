using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Application.Common;
using COEPD.SalesFunnelSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class DemoBookingService : IDemoBookingService
{
    private readonly IDemoBookingRepository _demoBookingRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IFunnelTrackingService _funnelTrackingService;
    private readonly IEmailAutomationService _emailAutomationService;
    private readonly IWhatsAppAutomationService _whatsAppAutomationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<DemoBookingService> _logger;

    public DemoBookingService(
        IDemoBookingRepository demoBookingRepository,
        ILeadRepository leadRepository,
        IFunnelTrackingService funnelTrackingService,
        IEmailAutomationService emailAutomationService,
        IWhatsAppAutomationService whatsAppAutomationService,
        IAnalyticsService analyticsService,
        ILogger<DemoBookingService> logger)
    {
        _demoBookingRepository = demoBookingRepository;
        _leadRepository = leadRepository;
        _funnelTrackingService = funnelTrackingService;
        _emailAutomationService = emailAutomationService;
        _whatsAppAutomationService = whatsAppAutomationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<DemoBookingResponse> CreateAsync(CreateDemoBookingRequest request, CancellationToken cancellationToken = default)
    {
        var leadId = request.LeadId.GetValueOrDefault();
        if (leadId <= 0)
        {
            throw new ArgumentException("LeadId is required and must be greater than zero.");
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken) ?? throw new NotFoundException("Lead not found.");

        var day = request.Day.Trim();
        var slot = request.Slot.Trim();

        var slotAvailable = await _demoBookingRepository.IsSlotAvailableAsync(day, slot, cancellationToken);
        if (!slotAvailable)
            throw new ConflictException("Selected slot is not available. Please choose another day/slot.");

        var normalizedStatus = string.IsNullOrWhiteSpace(request.Status) ? DemoBookingStatuses.Pending : request.Status.Trim();

        // Advance funnel stage when a demo is booked
        if (lead.FunnelStage is Domain.Entities.FunnelStages.New or Domain.Entities.FunnelStages.Contacted)
        {
            lead.FunnelStage = Domain.Entities.FunnelStages.DemoBooked;
            lead.Status = LeadStatuses.DemoBooked;
            await _leadRepository.UpdateAsync(lead, cancellationToken);
        }

        var created = await _demoBookingRepository.AddAsync(new DemoBooking
        {
            LeadId = leadId,
            Day = day,
            Slot = slot,
            Status = normalizedStatus
        }, cancellationToken);

        await _funnelTrackingService.TrackAsync(leadId, FunnelEventStages.Action, cancellationToken);
        _analyticsService.InvalidateDashboardCache();

        try
        {
            await _emailAutomationService.TriggerDemoReminderAsync(lead, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo reminder email failed for lead {LeadId}", lead.Id);
        }

        try
        {
            await _whatsAppAutomationService.SendDemoReminderAsync(lead, day, slot, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo reminder WhatsApp failed for lead {LeadId}", lead.Id);
        }

        return new DemoBookingResponse
        {
            Id = created.Id,
            LeadId = created.LeadId,
            Day = created.Day,
            Slot = created.Slot,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<DemoSlotAvailabilityResponse> CheckAvailabilityAsync(string day, string slot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(day))
        {
            throw new ArgumentException("Day is required.");
        }

        if (string.IsNullOrWhiteSpace(slot))
        {
            throw new ArgumentException("Slot is required.");
        }

        var normalizedDay = day.Trim();
        var normalizedSlot = slot.Trim();
        var isAvailable = await _demoBookingRepository.IsSlotAvailableAsync(normalizedDay, normalizedSlot, cancellationToken);

        return new DemoSlotAvailabilityResponse
        {
            Day = normalizedDay,
            Slot = normalizedSlot,
            IsAvailable = isAvailable
        };
    }

    public async Task<List<DemoBookingResponse>> GetByLeadIdAsync(int leadId, CancellationToken cancellationToken = default)
    {
        if (leadId <= 0)
        {
            throw new ArgumentException("LeadId is required and must be greater than zero.");
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            throw new NotFoundException("Lead not found.");
        }

        var bookings = await _demoBookingRepository.GetByLeadIdAsync(leadId, cancellationToken);
        return bookings.Select(x => new DemoBookingResponse
        {
            Id = x.Id,
            LeadId = x.LeadId,
            Day = x.Day,
            Slot = x.Slot,
            Status = x.Status,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<DemoBookingResponse> UpdateStatusAsync(int bookingId, UpdateDemoBookingStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentException("Booking id must be greater than zero.");
        }

        var booking = await _demoBookingRepository.GetByIdAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found.");
        var status = NormalizeStatus(request.Status);

        booking.Status = status;
        await _demoBookingRepository.UpdateAsync(booking, cancellationToken);
        _analyticsService.InvalidateDashboardCache();

        return new DemoBookingResponse
        {
            Id = booking.Id,
            LeadId = booking.LeadId,
            Day = booking.Day,
            Slot = booking.Slot,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt
        };
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "pending" => DemoBookingStatuses.Pending,
            "confirmed" => DemoBookingStatuses.Confirmed,
            _ => throw new ArgumentException("Invalid booking status. Allowed values: Pending, Confirmed.")
        };
    }
}
