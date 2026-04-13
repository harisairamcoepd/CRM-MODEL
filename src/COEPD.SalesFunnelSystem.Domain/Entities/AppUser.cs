using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class AppUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public ICollection<LeadActivityLog> LeadActivities { get; set; } = new List<LeadActivityLog>();
    public ICollection<LeadStageTransition> LeadStageTransitions { get; set; } = new List<LeadStageTransition>();
}
