using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using COEPD.SalesFunnelSystem.Domain.Entities;

namespace COEPD.SalesFunnelSystem.Application.Services;

public class ChatService : IChatService
{
    private static readonly string[] MenuReplies =
    [
        "Course Details", "Duration", "Placement", "Fees", "Batch Timings", "Book Free Demo", "Restart"
    ];

    private static readonly string[] DomainReplies =
    [
        "Business Analysis", "Data Analytics", "Product Management", "Agile & Scrum", "Healthcare BA", "Banking BA",
        "Insurance BA", "Supply Chain BA", "Retail BA", "Telecom BA", "ERP", "CRM", "Salesforce", "SAP", "Power BI",
        "SQL", "Python", "AI for Analysts", "QA Testing", "Project Management"
    ];

    private readonly IChatRepository _chatRepository;
    private readonly ILeadService _leadService;

    public ChatService(IChatRepository chatRepository, ILeadService leadService)
    {
        _chatRepository = chatRepository;
        _leadService = leadService;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var session = string.IsNullOrWhiteSpace(request.SessionId) ? null : await _chatRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            session = await _chatRepository.AddAsync(new ChatSession { Stage = "AskName", Source = request.Source }, cancellationToken);
            await SavePairAsync(session, request.Message, "Hi! Welcome to COEPD. What is your name?", cancellationToken);
            return new ChatResponse { SessionId = session.SessionId, Stage = session.Stage, Reply = "Hi! Welcome to COEPD. What is your name?" };
        }

        if (request.Message.Equals("Restart", StringComparison.OrdinalIgnoreCase))
        {
            session.Stage = "AskName";
            session.Name = session.Phone = session.Email = session.Location = session.Domain = null;
            session.LeadCaptured = false;
            await _chatRepository.UpdateAsync(session, cancellationToken);
            return new ChatResponse { SessionId = session.SessionId, Stage = session.Stage, Reply = "We restarted. What is your name?" };
        }

        var response = await HandleAsync(session, request.Message.Trim(), cancellationToken);
        await SavePairAsync(session, request.Message, response.Reply, cancellationToken);
        return response;
    }

    private async Task<ChatResponse> HandleAsync(ChatSession session, string message, CancellationToken cancellationToken)
    {
        switch (session.Stage)
        {
            case "AskName":
                session.Name = message; session.Stage = "AskPhone";
                await _chatRepository.UpdateAsync(session, cancellationToken);
                return Build(session, "Please share your phone number.");
            case "AskPhone":
                session.Phone = message; session.Stage = "AskEmail";
                await _chatRepository.UpdateAsync(session, cancellationToken);
                return Build(session, "What is your email address?");
            case "AskEmail":
                session.Email = message; session.Stage = "AskLocation";
                await _chatRepository.UpdateAsync(session, cancellationToken);
                return Build(session, "Which city are you joining from?");
            case "AskLocation":
                session.Location = message; session.Stage = "AskDomain";
                await _chatRepository.UpdateAsync(session, cancellationToken);
                return Build(session, "Which domain are you interested in?", DomainReplies);
            case "AskDomain":
                session.Domain = message; session.Stage = "Menu";
                if (!session.LeadCaptured)
                {
                    var lead = await _leadService.CreateAsync(new CreateLeadRequest
                    {
                        Name = session.Name ?? string.Empty,
                        Phone = session.Phone ?? string.Empty,
                        Email = session.Email ?? string.Empty,
                        Location = session.Location ?? string.Empty,
                        Domain = session.Domain,
                        Source = "Chatbot"
                    }, cancellationToken);

                    session.LeadCaptured = true;
                    await _chatRepository.UpdateAsync(session, cancellationToken);

                    return new ChatResponse
                    {
                        SessionId = session.SessionId,
                        Stage = session.Stage,
                        LeadCaptured = true,
                        LeadId = lead.Id,
                        Reply = $"Perfect. I've saved your details for {session.Domain}. What would you like to know next?",
                        QuickReplies = MenuReplies.ToList()
                    };
                }
                await _chatRepository.UpdateAsync(session, cancellationToken);
                return Build(session, "Choose one of the options below.", MenuReplies);
            default:
                return Build(session, message.ToLowerInvariant() switch
                {
                    "course details" => "COEPD offers structured, mentor-led programs with projects, case studies, and interview preparation.",
                    "duration" => "Most programs run between 8 and 16 weeks depending on the specialization.",
                    "placement" => "Placement support includes resume reviews, mock interviews, hiring partner connects, and career guidance.",
                    "fees" => "Fees vary by program and offers. An advisor can share the latest fee structure and EMI options.",
                    "batch timings" => "Weekday and weekend batches are available with morning and evening slots.",
                    "book free demo" => "Use the demo booking section on the page and submit your Lead ID to reserve a free demo.",
                    _ => "I can help with course details, duration, placement, fees, batch timings, or demo booking."
                }, MenuReplies);
        }
    }

    private static ChatResponse Build(ChatSession session, string reply, IEnumerable<string>? quickReplies = null) =>
        new() { SessionId = session.SessionId, Stage = session.Stage, Reply = reply, LeadCaptured = session.LeadCaptured, QuickReplies = quickReplies?.ToList() ?? new List<string>() };

    private async Task SavePairAsync(ChatSession session, string userMessage, string botMessage, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            await _chatRepository.SaveMessageAsync(new ChatMessage { ChatSessionId = session.Id, Sender = "User", Content = userMessage }, cancellationToken);
        }
        await _chatRepository.SaveMessageAsync(new ChatMessage { ChatSessionId = session.Id, Sender = "Bot", Content = botMessage }, cancellationToken);
    }
}
