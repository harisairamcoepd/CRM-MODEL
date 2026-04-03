using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class Lead : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = LeadStatuses.New;
    public string Score { get; set; } = LeadScores.Warm;
    public string? Notes { get; set; }
    public string FunnelStage { get; set; } = FunnelStages.New;
    public ICollection<DemoBooking> DemoBookings { get; set; } = new List<DemoBooking>();
}

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Demo = "Demo";
    public const string DemoBooked = "DemoBooked";
    public const string Converted = "Converted";
    public const string BookedLegacy = "Booked";
}

public static class LeadScores
{
    public const string Hot = "Hot";
    public const string Warm = "Warm";
    public const string Cold = "Cold";
}

public static class LeadSources
{
    public const string Website = "Website";
    public const string Chatbot = "Chatbot";
    public const string Ads = "Ads";
}

public static class FunnelStages
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string DemoBooked = "DemoBooked";
    public const string Enrolled = "Enrolled";
    public const string Lost = "Lost";
}
