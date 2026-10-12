using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditService audit,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit;
        _logger = logger;
    }

    // ── GET Login ─────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // ── POST Login ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            await _audit.LogAsync("Login", "Auth", details: $"Failed - user not found: {model.Email}",
                result: "Failed");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Contact your administrator.");
            await _audit.LogAsync("Login", "Auth", user.Id, details: "Deactivated account login attempt",
                result: "Failed");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _audit.LogAsync("Login", "Auth", user.Id, details: "Successful login", result: "Success");
            _logger.LogInformation("User {Email} logged in.", model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _audit.LogAsync("Lockout", "Auth", user?.Id, details: $"Account locked: {model.Email}",
                result: "Lockout");
            return View("Lockout");
        }

        await _audit.LogAsync("Login", "Auth", user?.Id,
            details: $"Failed login attempt for {model.Email}", result: "Failed");
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    // ── GET Register ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    // ── POST Register ─────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // New self-registrations are always SalesExecutive — never Admin
            await _userManager.AddToRoleAsync(user, AppRoles.SalesExecutive);
            await _signInManager.SignInAsync(user, isPersistent: false);
            await _audit.LogAsync("Register", "Auth", user.Id,
                newValue: user.Email, details: "Self-registration as SalesExecutive");
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // ── POST Logout ───────────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _audit.LogAsync("Logout", "Auth", userId, details: "User logged out");
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    // ── Access Denied ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ── Lockout ────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Lockout() => View();
}
