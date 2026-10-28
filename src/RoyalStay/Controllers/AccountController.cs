using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RoyalStay.Data;
using RoyalStay.Models;
using RoyalStay.Security;

namespace RoyalStay.Controllers;

public class AccountController : Controller
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(5);

    // A real hash of a random string, used so "unknown email" takes the same time as "wrong password".
    private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString());

    private readonly UserRepository _users;
    private readonly IMemoryCache _cache;

    public AccountController(UserRepository users, IMemoryCache cache)
    {
        _users = users;
        _cache = cache;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Admin");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLowerInvariant();
        var key = "login-fail:" + email;
        var failures = _cache.TryGetValue<int>(key, out var f) ? f : 0;
        if (failures >= MaxFailedAttempts)
        {
            ModelState.AddModelError("", "Too many failed attempts. Please try again in a few minutes.");
            return View(model);
        }

        var admin = _users.FindAdminByEmail(email);
        // Always run one hash verification so timing does not reveal whether the email exists.
        var ok = PasswordHasher.Verify(model.Password, admin?.PasswordHash ?? DummyHash) && admin != null;

        if (!ok)
        {
            _cache.Set(key, failures + 1, LockoutTime);
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        _cache.Remove(key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin!.UserId.ToString()),
            new(ClaimTypes.Name, admin.FullName),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Role, "Admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false });

        // Only redirect to local URLs (prevents open-redirect attacks).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
