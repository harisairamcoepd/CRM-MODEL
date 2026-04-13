using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class LeadStageTransition : BaseEntity
{
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public AppUser? ChangedByUser { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
