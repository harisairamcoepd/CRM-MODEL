# // INTEGRATION NOTE
# Run the EF Core scaffold command from the solution root after reviewing the entity and DbContext changes below.
# The migration adds stage-tracking persistence used by the Kanban board, 3D funnel analytics, SignalR payloads, and response-time stats.

## EF Core Migration Scaffold Command

```powershell
dotnet ef migrations add AddUiFeatureStageTracking `
  --project src/COEPD.SalesFunnelSystem.Infrastructure/COEPD.SalesFunnelSystem.Infrastructure.csproj `
  --startup-project src/COEPD.SalesFunnelSystem.Web/COEPD.SalesFunnelSystem.Web.csproj `
  --context ApplicationDbContext
```

## Schema Impact Summary

- Adds `Lead.StageEnteredAtUtc`
- Adds `Lead.FirstRespondedAtUtc`
- Adds `LeadStageTransitions` table with audit columns, `FromStage`, `ToStage`, `ChangedByUserId`, and `ChangedAtUtc`
- Adds `DbSet<LeadStageTransition>` to `ApplicationDbContext`
- Adds indexes for `Lead.StageEnteredAtUtc` and `LeadStageTransition.ToStage + ChangedAtUtc`

## Runtime Wiring Summary

- Register SignalR with `builder.Services.AddSignalR()`
- Map `LeadHub` with `app.MapHub<LeadHub>(LeadHub.HubPath)`
- Broadcast `LeadStageChangedBroadcast` after a successful stage transition save
- Implement the abstract API signatures in `src/COEPD.SalesFunnelSystem.Web/Controllers/Api/UiFeatureContractControllers.cs`
