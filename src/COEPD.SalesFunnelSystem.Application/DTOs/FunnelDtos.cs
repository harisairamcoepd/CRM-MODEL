namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class FunnelEventRequest
{
    public int LeadId { get; set; }
    public string Stage { get; set; } = string.Empty;
}

public class FunnelStageCountResponse
{
    public int Awareness { get; set; }
    public int Interest { get; set; }
    public int Desire { get; set; }
    public int Action { get; set; }
}

public class FunnelAnalyticsResponse
{
    public FunnelStageCountResponse StageCounts { get; set; } = new();
    public decimal AwarenessToInterestRate { get; set; }
    public decimal InterestToDesireRate { get; set; }
    public decimal DesireToActionRate { get; set; }
    public decimal OverallConversionRate { get; set; }
    public List<FunnelTrendPoint> Trend { get; set; } = new();
}

public class FunnelTrendPoint
{
    public string Date { get; set; } = string.Empty;
    public int Awareness { get; set; }
    public int Interest { get; set; }
    public int Desire { get; set; }
    public int Action { get; set; }
}
