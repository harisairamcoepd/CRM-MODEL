using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace COEPD.SalesFunnelSystem.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class StaffController : Controller
{
    private readonly ILeadService _leadService;
    public StaffController(ILeadService leadService) => _leadService = leadService;
    public async Task<IActionResult> Index([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
    {
        var allLeads = await _leadService.GetAllAsync(new LeadFilterRequest(), cancellationToken);
        ViewBag.Filter = filter;
        ViewBag.Domains = allLeads.Select(x => x.Domain).Distinct().OrderBy(x => x).ToList();

        List<LeadResponse> visibleLeads;
        if (User.IsInRole("Admin"))
        {
            visibleLeads = await _leadService.GetAllAsync(filter, cancellationToken);
        }
        else
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!int.TryParse(rawUserId, out var staffId))
            {
                return Forbid();
            }

            visibleLeads = await _leadService.GetAssignedAsync(staffId, cancellationToken);
            visibleLeads = ApplyFilter(visibleLeads, filter);
        }

        return View(visibleLeads);
    }

    private static List<LeadResponse> ApplyFilter(List<LeadResponse> leads, LeadFilterRequest filter)
    {
        IEnumerable<LeadResponse> query = leads;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Domain.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Domain))
        {
            query = query.Where(x => string.Equals(x.Domain, filter.Domain, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endDateExclusive = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < endDateExclusive);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToList();
    }
}
