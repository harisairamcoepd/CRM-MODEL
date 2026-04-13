// INTEGRATION NOTE
// Inject IHubContext<LeadHub> into the service or controller that changes lead stages,
// then call LeadHub.BroadcastStageChangedAsync(...) after the database transaction succeeds.
// The _Kanban.cshtml partial listens for the "LeadStageChanged" event on /hubs/leads.

using COEPD.SalesFunnelSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace COEPD.SalesFunnelSystem.Web.Hubs;

[Authorize]
public class LeadHub : Hub
{
    public const string HubPath = "/hubs/leads";
    public const string PipelineGroup = "lead-pipeline";

    public Task JoinPipeline()
        => Groups.AddToGroupAsync(Context.ConnectionId, PipelineGroup);

    public Task LeavePipeline()
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, PipelineGroup);

    public static Task BroadcastStageChangedAsync(
        IHubContext<LeadHub> hubContext,
        LeadStageChangedBroadcast payload,
        CancellationToken cancellationToken = default)
        => hubContext.Clients.Group(PipelineGroup)
            .SendAsync("LeadStageChanged", payload, cancellationToken);
}
