using System.Security.Claims;
using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleHome(User.FindFirstValue(ClaimTypes.Role));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl, CancellationToken cancellationToken)
    {
        var auth = await _authService.LoginAsync(request, cancellationToken);
        if (auth is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(request);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new("access_token", auth.Token)
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(request.RememberMe ? 24 : 8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            authProperties);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToRoleHome(auth.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Denied() => View();

    private IActionResult RedirectToRoleHome(string? role) =>
        string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Staff");
}
