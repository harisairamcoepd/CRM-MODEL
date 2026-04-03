namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class CreateLeadRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Score { get; set; } = "Warm";
    public string? Notes { get; set; }
}

public class LeadResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string FunnelStage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LeadFilterRequest
{
    public string? Search { get; set; }
    public string? Source { get; set; }
    public string? Status { get; set; }
    public string? Domain { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public class UpdateLeadStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateLeadStatusResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FunnelStage { get; set; } = string.Empty;
}

public class PipelineStageBucket
{
    public string Stage { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ProgressPercent { get; set; }
    public int Count { get; set; }
    public List<LeadResponse> Leads { get; set; } = [];
}

public class PipelineBoardResponse
{
    public List<PipelineStageBucket> Stages { get; set; } = [];
}

public class MoveLeadStageRequest
{
    public string Stage { get; set; } = string.Empty;
}
