using COEPD.SalesFunnelSystem.Domain.Common;

namespace COEPD.SalesFunnelSystem.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public int ChatSessionId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ChatSession? ChatSession { get; set; }
}
