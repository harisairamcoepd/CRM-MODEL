namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class DashboardStatsResponse
{
    public int TotalLeads { get; set; }
    public int TodayLeads { get; set; }
    public int ConversionCount { get; set; }
    public int ThisMonthLeads { get; set; }
    public int TotalBookings { get; set; }
    public decimal WeeklyGrowthPercentage { get; set; }
    public Dictionary<string, int> SourceBreakdown { get; set; } = new();
}

public class LeadStatsResponse
{
    public int TotalLeads { get; set; }
    public int TodayLeads { get; set; }
    public int ThisMonthLeads { get; set; }
    public int DemoBookedLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal ConversionRate { get; set; }
    public Dictionary<string, int> StatusBreakdown { get; set; } = new();
}

public class LeadGrowthPoint
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class LeadAnalyticsResponse
{
    public int TotalLeads { get; set; }
    public int NewLeads { get; set; }
    public int ContactedLeads { get; set; }
    public int DemoLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal ConversionRate { get; set; }
    public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    public Dictionary<string, int> SourceBreakdown { get; set; } = new();
}
