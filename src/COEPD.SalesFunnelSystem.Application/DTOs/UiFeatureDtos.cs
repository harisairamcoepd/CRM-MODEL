namespace COEPD.SalesFunnelSystem.Application.DTOs;

public sealed class Funnel3DStageDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? HexColor { get; set; }
}

public sealed class KanbanLeadCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string? AssignedStaffName { get; set; }
    public string? Domain { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime StageEnteredAtUtc { get; set; }
}

public sealed class LeadStagePatchRequest
{
    public string Stage { get; set; } = string.Empty;
}

public sealed class LeadStageChangedBroadcast
{
    public int LeadId { get; set; }
    public string PreviousStage { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? AssignedStaffName { get; set; }
    public DateTime StageEnteredAtUtc { get; set; }
    public int DaysInStage { get; set; }
}

public sealed class StatTrendDto
{
    public decimal PercentageDelta { get; set; }
    public string Direction { get; set; } = "flat";
    public string Label { get; set; } = string.Empty;
}

public sealed class StatsSummaryWidgetDto
{
    public int TotalLeads { get; set; }
    public int DemosBooked { get; set; }
    public int Converted { get; set; }
    public double AvgResponseTimeMinutes { get; set; }
    public StatTrendDto TotalLeadsTrend { get; set; } = new();
    public StatTrendDto DemosBookedTrend { get; set; } = new();
    public StatTrendDto ConvertedTrend { get; set; } = new();
    public StatTrendDto AvgResponseTimeTrend { get; set; } = new();
}
