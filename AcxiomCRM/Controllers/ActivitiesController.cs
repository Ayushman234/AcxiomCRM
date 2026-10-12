using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public ActivitiesController(
        IActivityService activityService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _activityService = activityService;
        _userManager = userManager;
        _db = db;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    public async Task<IActionResult> Index(ActivityFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var (items, total) = await _activityService.GetPagedAsync(filter, userId, role);
        ViewBag.Filter = filter;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / (filter.PageSize < 1 ? 10 : filter.PageSize));
        return View(items);
    }

    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        var vm = new ActivityVM
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.Today,
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var activity = new Activity
        {
            ActivityType = vm.ActivityType,
            Subject = vm.Subject,
            Description = vm.Description,
            ActivityDate = vm.ActivityDate,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            AssignedTo = vm.AssignedTo,
            Status = vm.Status,
            CreatedBy = user!.Id
        };

        var (success, error) = await _activityService.CreateAsync(activity);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Activity recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null) return NotFound();

        var vm = new ActivityVM
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            AssignedTo = activity.AssignedTo,
            Status = activity.Status,
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityVM vm)
    {
        if (id != vm.ActivityId) return BadRequest();
        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var activity = new Activity
        {
            ActivityId = vm.ActivityId,
            ActivityType = vm.ActivityType,
            Subject = vm.Subject,
            Description = vm.Description,
            ActivityDate = vm.ActivityDate,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            AssignedTo = vm.AssignedTo,
            Status = vm.Status
        };

        var (success, error) = await _activityService.UpdateAsync(activity);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Activity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _activityService.DeleteAsync(id);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Activity cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCustomersAsync() =>
        await _db.Customers.OrderBy(c => c.CustomerName)
            .Select(c => new SelectListItem(c.CustomerName, c.CustomerId.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> GetLeadsAsync() =>
        await _db.Leads.OrderBy(l => l.LeadName)
            .Select(l => new SelectListItem(l.LeadName, l.LeadId.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> GetSalesExecutivesAsync()
    {
        var executives = await _userManager.GetUsersInRoleAsync(AppRoles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(AppRoles.Manager);
        return executives.Concat(managers)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FullName, u.Id)).ToList();
    }
}
