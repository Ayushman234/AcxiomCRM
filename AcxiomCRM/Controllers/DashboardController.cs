using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboard, UserManager<ApplicationUser> userManager)
    {
        _dashboard = dashboard;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.SalesExecutive;

        var vm = await _dashboard.GetDashboardDataAsync(user.Id, role);
        ViewBag.UserRole = role;
        ViewBag.UserName = user.FullName;
        return View(vm);
    }
}
