using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class LeadFollowUpJob : BaseEntity
{
    public int LeadId { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = FollowUpJobStatuses.Pending;
    public int AttemptCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public static class FollowUpJobTypes
{
    public const string OneHour = "OneHour";
    public const string OneDay = "OneDay";
}

public static class FollowUpJobStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
