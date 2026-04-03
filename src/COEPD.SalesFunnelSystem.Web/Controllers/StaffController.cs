using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class StaffController : Controller
{
    private readonly ILeadService _leadService;
    public StaffController(ILeadService leadService) => _leadService = leadService;
    public async Task<IActionResult> Index([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
    {
        var allLeads = await _leadService.GetAllAsync(new LeadFilterRequest(), cancellationToken);
        ViewBag.ApiToken = User.Claims.FirstOrDefault(x => x.Type == "access_token")?.Value ?? string.Empty;
        ViewBag.Filter = filter;
        ViewBag.Domains = allLeads.Select(x => x.Domain).Distinct().OrderBy(x => x).ToList();
        return View(await _leadService.GetAllAsync(filter, cancellationToken));
    }
}
