using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class FunnelEvent : BaseEntity
{
    public int LeadId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class FunnelEventStages
{
    public const string Awareness = "Awareness";
    public const string Interest = "Interest";
    public const string Desire = "Desire";
    public const string Action = "Action";
}
