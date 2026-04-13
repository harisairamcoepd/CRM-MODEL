using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers;

[Authorize(Policy = "AdminOnly", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILeadService _leadService;
    private readonly IFunnelTrackingService _funnelTrackingService;

    public AdminController(IAnalyticsService analyticsService, ILeadService leadService, IFunnelTrackingService funnelTrackingService)
    {
        _analyticsService = analyticsService;
        _leadService = leadService;
        _funnelTrackingService = funnelTrackingService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
    {
        var leads = await _leadService.GetAllAsync(filter, cancellationToken);
        var allLeads = await _leadService.GetAllAsync(new LeadFilterRequest(), cancellationToken);

        ViewBag.Stats = await _analyticsService.GetStatsAsync(cancellationToken);
        ViewBag.Growth = await _analyticsService.GetLeadGrowthAsync(14, cancellationToken);
        ViewBag.Funnel = await _funnelTrackingService.GetAnalyticsAsync(14, cancellationToken);
        ViewBag.Filter = filter;
        ViewBag.Domains = allLeads.Select(x => x.Domain).Distinct().OrderBy(x => x).ToList();
        return View(leads);
    }

    [HttpGet("leads")]
    public async Task<IActionResult> Leads([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
    {
        var leads = await _leadService.GetAllAsync(filter, cancellationToken);
        var allLeads = await _leadService.GetAllAsync(new LeadFilterRequest(), cancellationToken);

        ViewBag.Filter = filter;
        ViewBag.Domains = allLeads.Select(x => x.Domain).Distinct().OrderBy(x => x).ToList();
        return View(leads);
    }
}
