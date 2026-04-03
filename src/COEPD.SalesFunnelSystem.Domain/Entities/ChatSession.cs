using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class ChatSession : BaseEntity
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Stage { get; set; } = "Welcome";
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? Domain { get; set; }
    public bool LeadCaptured { get; set; }
    public string Source { get; set; } = "Website";
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
