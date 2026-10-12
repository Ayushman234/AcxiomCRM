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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _oppService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public OpportunitiesController(
        IOpportunityService oppService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _oppService = oppService;
        _userManager = userManager;
        _db = db;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    public async Task<IActionResult> Index(OpportunityFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var (items, total) = await _oppService.GetPagedAsync(filter, userId, role);
        ViewBag.Filter = filter;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / (filter.PageSize < 1 ? 10 : filter.PageSize));
        ViewBag.TotalCount = total;
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var opp = await _oppService.GetByIdAsync(id);
        if (opp == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            opp.AssignedTo != userId && opp.CreatedBy != userId)
            return Forbid();

        return View(opp);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new OpportunityVM
        {
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            SalesExecutives = await GetSalesExecutivesAsync(),
            ExpectedCloseDate = DateTime.Today.AddMonths(1)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityVM vm)
    {
        // Additional server-side business rules
        if (vm.Amount <= 0)
            ModelState.AddModelError(nameof(vm.Amount), "Opportunity Amount must be greater than 0.");
        if (vm.Probability < 0 || vm.Probability > 100)
            ModelState.AddModelError(nameof(vm.Probability), "Probability must be between 0 and 100.");
        if (vm.Status == OpportunityStatus.Open && vm.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(vm.ExpectedCloseDate), "Expected Close Date cannot be in the past.");

        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var opp = new Opportunity
        {
            OpportunityName = vm.OpportunityName,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Amount = vm.Amount,
            Stage = vm.Stage,
            Status = vm.Status,
            Probability = vm.Probability,
            ExpectedCloseDate = vm.ExpectedCloseDate,
            Source = vm.Source,
            Notes = vm.Notes,
            AssignedTo = vm.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _oppService.CreateAsync(opp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = $"Opportunity '{vm.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _oppService.GetByIdAsync(id);
        if (opp == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            opp.AssignedTo != userId && opp.CreatedBy != userId)
            return Forbid();

        var vm = new OpportunityVM
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Status = opp.Status,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Source = opp.Source,
            Notes = opp.Notes,
            AssignedTo = opp.AssignedTo,
            Customers = await GetCustomersAsync(),
            Leads = await GetLeadsAsync(),
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityVM vm)
    {
        if (id != vm.OpportunityId) return BadRequest();

        if (vm.Amount <= 0)
            ModelState.AddModelError(nameof(vm.Amount), "Opportunity Amount must be greater than 0.");
        if (vm.Probability < 0 || vm.Probability > 100)
            ModelState.AddModelError(nameof(vm.Probability), "Probability must be between 0 and 100.");
        if (vm.Status == OpportunityStatus.Open && vm.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(vm.ExpectedCloseDate), "Expected Close Date cannot be in the past.");

        if (!ModelState.IsValid)
        {
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var opp = new Opportunity
        {
            OpportunityId = vm.OpportunityId,
            OpportunityName = vm.OpportunityName,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Amount = vm.Amount,
            Stage = vm.Stage,
            Status = vm.Status,
            Probability = vm.Probability,
            ExpectedCloseDate = vm.ExpectedCloseDate,
            Source = vm.Source,
            Notes = vm.Notes,
            AssignedTo = vm.AssignedTo
        };

        var (success, error) = await _oppService.UpdateAsync(opp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.Customers = await GetCustomersAsync();
            vm.Leads = await GetLeadsAsync();
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _oppService.DeleteAsync(id);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Opportunity marked as Lost.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetCustomersAsync() =>
        await _db.Customers.Where(c => c.Status == CustomerStatus.Active)
            .OrderBy(c => c.CustomerName)
            .Select(c => new SelectListItem(c.CustomerName, c.CustomerId.ToString()))
            .ToListAsync();

    private async Task<List<SelectListItem>> GetLeadsAsync() =>
        await _db.Leads.Where(l => l.Status == LeadStatus.Qualified || l.Status == LeadStatus.Contacted)
            .OrderBy(l => l.LeadName)
            .Select(l => new SelectListItem(l.LeadName, l.LeadId.ToString()))
            .ToListAsync();

    private async Task<List<SelectListItem>> GetSalesExecutivesAsync()
    {
        var executives = await _userManager.GetUsersInRoleAsync(AppRoles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(AppRoles.Manager);
        return executives.Concat(managers)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FullName, u.Id)).ToList();
    }
}
