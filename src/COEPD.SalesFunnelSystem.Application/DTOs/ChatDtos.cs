namespace COEPD.SalesFunnelSystem.Application.DTOs;

public class ChatRequest
{
    public string? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "Website";
}

public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public List<string> QuickReplies { get; set; } = new();
    public bool LeadCaptured { get; set; }
    public int? LeadId { get; set; }
}
