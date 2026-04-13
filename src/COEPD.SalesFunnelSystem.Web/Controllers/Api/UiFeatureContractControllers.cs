// INTEGRATION NOTE
// These abstract controllers define the new API endpoints required by the 3D funnel,
// Kanban drag-drop persistence, and animated stats widget.
// Implement them in concrete controllers or merge the signatures into your existing controllers.

using COEPD.SalesFunnelSystem.Application.DTOs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers.Api;

[ApiController]
[Route("api/funnel")]
public abstract class FunnelVisualizationContractController : ControllerBase
{
    [HttpGet("3d")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public abstract Task<ActionResult<IReadOnlyList<Funnel3DStageDto>>> Get3DStageVolumes(CancellationToken cancellationToken);
}

[ApiController]
[Route("api/leads")]
public abstract class LeadStageContractController : ControllerBase
{
    [HttpPatch("{id:int}/stage")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public abstract Task<ActionResult<LeadStageChangedBroadcast>> PatchStage(
        int id,
        [FromBody] LeadStagePatchRequest request,
        CancellationToken cancellationToken);
}

[ApiController]
[Route("api/stats")]
public abstract class StatsSummaryContractController : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public abstract Task<ActionResult<StatsSummaryWidgetDto>> GetSummary(CancellationToken cancellationToken);
}
