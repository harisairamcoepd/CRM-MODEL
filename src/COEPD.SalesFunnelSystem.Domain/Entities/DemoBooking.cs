using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class DemoBooking : BaseEntity
{
    public int LeadId { get; set; }
    public string Day { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public string Status { get; set; } = DemoBookingStatuses.Pending;
    public Lead? Lead { get; set; }
}

public static class DemoBookingStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
}
