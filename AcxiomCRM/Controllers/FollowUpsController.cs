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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public FollowUpsController(
        IFollowUpService followUpService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _followUpService = followUpService;
        _userManager = userManager;
        _db = db;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    public async Task<IActionResult> Index(FollowUpFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var (items, total) = await _followUpService.GetPagedAsync(filter, userId, role);
        var overdue = await _followUpService.GetOverdueAsync(userId, role);
        ViewBag.Filter = filter;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / (filter.PageSize < 1 ? 10 : filter.PageSize));
        ViewBag.TotalCount = total;
        ViewBag.OverdueCount = overdue.Count;
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var followUp = await _followUpService.GetByIdAsync(id);
        if (followUp == null) return NotFound();
        return View(followUp);
    }

    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        var vm = new FollowUpVM
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.Today,
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            Opportunities = await GetOpportunitiesAsync(),
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpVM vm)
    {
        // Server-side: follow-up date cannot be before today for planned
        if (vm.Status == FollowUpStatus.Planned && vm.FollowUpDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(vm.FollowUpDate),
                "Follow-up date cannot be earlier than today.");

        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.Opportunities = await GetOpportunitiesAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var followUp = new FollowUp
        {
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            OpportunityId = vm.OpportunityId,
            FollowUpDate = vm.FollowUpDate,
            FollowUpType = vm.FollowUpType,
            Subject = vm.Subject,
            Remarks = vm.Remarks,
            Status = vm.Status,
            AssignedTo = vm.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _followUpService.CreateAsync(followUp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.Opportunities = await GetOpportunitiesAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _followUpService.GetByIdAsync(id);
        if (followUp == null) return NotFound();

        var vm = new FollowUpVM
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            Opportunities = await GetOpportunitiesAsync(),
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpVM vm)
    {
        if (id != vm.FollowUpId) return BadRequest();

        if ((vm.Status == FollowUpStatus.Planned) && vm.FollowUpDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(vm.FollowUpDate),
                "Follow-up date cannot be earlier than today.");

        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.Opportunities = await GetOpportunitiesAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var followUp = new FollowUp
        {
            FollowUpId = vm.FollowUpId,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            OpportunityId = vm.OpportunityId,
            FollowUpDate = vm.FollowUpDate,
            FollowUpType = vm.FollowUpType,
            Subject = vm.Subject,
            Remarks = vm.Remarks,
            Status = vm.Status,
            AssignedTo = vm.AssignedTo
        };

        var (success, error) = await _followUpService.UpdateAsync(followUp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.Opportunities = await GetOpportunitiesAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string notes)
    {
        var (success, error) = await _followUpService.CompleteAsync(id, notes);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Follow-up marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (success, error) = await _followUpService.CancelAsync(id);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Follow-up cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCustomersAsync() =>
        await _db.Customers.OrderBy(c => c.CustomerName)
            .Select(c => new SelectListItem(c.CustomerName, c.CustomerId.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> GetLeadsAsync() =>
        await _db.Leads.OrderBy(l => l.LeadName)
            .Select(l => new SelectListItem(l.LeadName, l.LeadId.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> GetOpportunitiesAsync() =>
        await _db.Opportunities.OrderBy(o => o.OpportunityName)
            .Select(o => new SelectListItem(o.OpportunityName, o.OpportunityId.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> GetSalesExecutivesAsync()
    {
        var executives = await _userManager.GetUsersInRoleAsync(AppRoles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(AppRoles.Manager);
        return executives.Concat(managers)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FullName, u.Id)).ToList();
    }
}
