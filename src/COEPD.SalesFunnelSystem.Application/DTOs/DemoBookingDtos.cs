namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class CreateDemoBookingRequest
{
    public int? LeadId { get; set; }
    public string Day { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class DemoBookingResponse
{
    public int Id { get; set; }
    public int LeadId { get; set; }
    public string Day { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DemoSlotAvailabilityResponse
{
    public string Day { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public class UpdateDemoBookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
