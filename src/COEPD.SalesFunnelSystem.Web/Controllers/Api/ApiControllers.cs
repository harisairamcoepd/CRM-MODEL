using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthApiController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var auth = await _authService.LoginAsync(request, cancellationToken);
        if (auth is null)
        {
            return Unauthorized(new { success = false, message = "Invalid credentials." });
        }

        return Ok(auth);
    }
}

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    public ChatController(IChatService chatService) => _chatService = chatService;
    [HttpPost] public async Task<IActionResult> Post(ChatRequest request, CancellationToken cancellationToken) => Ok(await _chatService.SendMessageAsync(request, cancellationToken));
}

[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;
    public LeadsController(ILeadService leadService) => _leadService = leadService;

    /// <summary>Public lead capture endpoint. No auth required so the landing page form works.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Post([FromBody] CreateLeadRequest request, CancellationToken cancellationToken)
    {
        request.Source = ResolveSource(request.Source);
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            request.Status = Domain.Entities.LeadStatuses.New;
        }

        var lead = await _leadService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, new
        {
            success = true,
            message = "Lead saved. Email, WhatsApp, staff notification, and follow-up automations triggered successfully.",
            lead
        });
    }

    private string ResolveSource(string? requestedSource)
    {
        if (!string.IsNullOrWhiteSpace(requestedSource))
        {
            return requestedSource;
        }

        var explicitHeader = Request.Headers["X-Lead-Source"].ToString();
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return explicitHeader;
        }

        var clientApp = Request.Headers["X-Client-App"].ToString();
        if (clientApp.Equals("chatbot", StringComparison.OrdinalIgnoreCase))
        {
            return Domain.Entities.LeadSources.Chatbot;
        }

        var utmSource = Request.Query["utm_source"].ToString();
        if (!string.IsNullOrWhiteSpace(utmSource))
        {
            if (utmSource.Contains("ad", StringComparison.OrdinalIgnoreCase) || utmSource.Contains("campaign", StringComparison.OrdinalIgnoreCase))
            {
                return Domain.Entities.LeadSources.Ads;
            }
        }

        var referer = Request.Headers.Referer.ToString();
        if (referer.Contains("chat", StringComparison.OrdinalIgnoreCase))
        {
            return Domain.Entities.LeadSources.Chatbot;
        }

        if (referer.Contains("ad", StringComparison.OrdinalIgnoreCase) || referer.Contains("campaign", StringComparison.OrdinalIgnoreCase))
        {
            return Domain.Entities.LeadSources.Ads;
        }

        return Domain.Entities.LeadSources.Website;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
        => Ok(await _leadService.GetAllAsync(filter, cancellationToken));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _leadService.GetByIdAsync(id, cancellationToken));

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeadStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _leadService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Lead status updated successfully.",
            lead = result
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _leadService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/demo")]
public class DemoBookingController : ControllerBase
{
    private readonly IDemoBookingService _demoBookingService;
    public DemoBookingController(IDemoBookingService demoBookingService) => _demoBookingService = demoBookingService;

    [HttpGet("availability")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability([FromQuery] string day, [FromQuery] string slot, CancellationToken cancellationToken)
    {
        var availability = await _demoBookingService.CheckAvailabilityAsync(day, slot, cancellationToken);
        return Ok(new
        {
            success = true,
            availability
        });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Post([FromBody] CreateDemoBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await _demoBookingService.CreateAsync(request, cancellationToken);
        var confirmationCode = $"DB-{booking.Id:D6}";

        return Created(string.Empty, new
        {
            success = true,
            message = "Demo booking created successfully.",
            bookingId = booking.Id,
            confirmation = new
            {
                confirmationCode,
                leadId = booking.LeadId,
                day = booking.Day,
                slot = booking.Slot,
                status = booking.Status
            },
            booking
        });
    }

    [HttpGet("lead/{leadId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByLeadId(int leadId, CancellationToken cancellationToken)
        => Ok(await _demoBookingService.GetByLeadIdAsync(leadId, cancellationToken));

    [HttpPut("{bookingId:int}/status")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateStatus(int bookingId, [FromBody] UpdateDemoBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var booking = await _demoBookingService.UpdateStatusAsync(bookingId, request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Booking status updated successfully.",
            booking
        });
    }
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    public DashboardController(IAnalyticsService analyticsService) => _analyticsService = analyticsService;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken) => Ok(await _analyticsService.GetStatsAsync(cancellationToken));

    [HttpGet("lead-growth")]
    public async Task<IActionResult> GetLeadGrowth(CancellationToken cancellationToken) => Ok(await _analyticsService.GetLeadGrowthAsync(cancellationToken));

    [HttpGet("lead-analytics")]
    public async Task<IActionResult> GetLeadAnalytics(CancellationToken cancellationToken) => Ok(await _analyticsService.GetLeadAnalyticsAsync(cancellationToken));
}

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _userService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return Created($"/api/users/{user.Id}", new
        {
            success = true,
            message = "User created successfully.",
            user
        });
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateRoleAsync(id, request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "User role updated successfully.",
            user
        });
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "User status updated successfully.",
            user
        });
    }
}

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    public StatsController(IAnalyticsService analyticsService) => _analyticsService = analyticsService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var stats = await _analyticsService.GetStatsAsync(cancellationToken);
        return Ok(new
        {
            totalLeads = stats.TotalLeads,
            todayLeads = stats.TodayLeads,
            conversionCount = stats.ConversionCount,
            thisMonthLeads = stats.ThisMonthLeads,
            totalBookings = stats.TotalBookings,
            weeklyGrowthPercentage = stats.WeeklyGrowthPercentage,
            sourceBreakdown = stats.SourceBreakdown
        });
    }
}

[ApiController]
[Route("api/funnel")]
public class FunnelController : ControllerBase
{
    private readonly IFunnelTrackingService _funnelTrackingService;
    public FunnelController(IFunnelTrackingService funnelTrackingService) => _funnelTrackingService = funnelTrackingService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _funnelTrackingService.GetCountsAsync(cancellationToken));

    [HttpGet("analytics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAnalytics([FromQuery] int days = 30, CancellationToken cancellationToken = default)
        => Ok(await _funnelTrackingService.GetAnalyticsAsync(days, cancellationToken));

    [HttpPost("event")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackEvent([FromBody] FunnelEventRequest request, CancellationToken cancellationToken)
    {
        await _funnelTrackingService.TrackAsync(request.LeadId, request.Stage, cancellationToken);
        return Ok(new { success = true, message = "Funnel event tracked." });
    }
}

[ApiController]
[Route("api/pipeline")]
public class PipelineController : ControllerBase
{
    private readonly ILeadService _leadService;

    public PipelineController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Get([FromQuery] LeadFilterRequest filter, CancellationToken cancellationToken)
        => Ok(await _leadService.GetPipelineAsync(filter, cancellationToken));

    [HttpPut("{id:int}/move")]
    [Authorize(Policy = "StaffOrAdmin", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Move(int id, [FromBody] MoveLeadStageRequest request, CancellationToken cancellationToken)
    {
        var lead = await _leadService.MoveToStageAsync(id, request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Lead moved successfully.",
            lead
        });
    }
}
