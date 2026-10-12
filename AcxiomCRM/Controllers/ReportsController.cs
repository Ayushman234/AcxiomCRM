using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Customers(ReportFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var data = await _reportService.GetCustomerReportAsync(filter, userId, role);
        ViewBag.Filter = filter;
        return View(data);
    }

    public async Task<IActionResult> Leads(ReportFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var data = await _reportService.GetLeadReportAsync(filter, userId, role);
        ViewBag.Filter = filter;
        return View(data);
    }

    public async Task<IActionResult> Opportunities(ReportFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var data = await _reportService.GetOpportunityReportAsync(filter, userId, role);
        ViewBag.Filter = filter;
        return View(data);
    }

    public async Task<IActionResult> FollowUps(ReportFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var data = await _reportService.GetFollowUpReportAsync(filter, userId, role);
        ViewBag.Filter = filter;
        return View(data);
    }

    public async Task<IActionResult> Pipeline()
    {
        var (userId, role) = await GetUserContextAsync();
        var data = await _reportService.GetPipelineReportAsync(userId, role);
        return View(data);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Audit(AuditFilterVM filter)
    {
        var data = await _reportService.GetAuditReportAsync(filter);
        ViewBag.Filter = filter;
        return View(data);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public Task<IActionResult> AuditLogs(AuditFilterVM filter) => Audit(filter);
}
