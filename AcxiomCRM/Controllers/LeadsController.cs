using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    public async Task<IActionResult> Index(LeadFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var (items, total) = await _leadService.GetPagedAsync(filter, userId, role);
        ViewBag.Filter = filter;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / (filter.PageSize < 1 ? 10 : filter.PageSize));
        ViewBag.TotalCount = total;
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            lead.AssignedTo != userId && lead.CreatedBy != userId)
            return Forbid();

        return View(lead);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new LeadVM { SalesExecutives = await GetSalesExecutivesAsync() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var lead = new Lead
        {
            LeadName = vm.LeadName,
            Email = vm.Email,
            Phone = vm.Phone,
            CompanyName = vm.CompanyName,
            Source = vm.Source,
            Priority = vm.Priority,
            ExpectedValue = vm.ExpectedValue,
            Notes = vm.Notes,
            AssignedTo = vm.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _leadService.CreateAsync(lead);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = $"Lead '{vm.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            lead.AssignedTo != userId && lead.CreatedBy != userId)
            return Forbid();

        var vm = new LeadVM
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedTo = lead.AssignedTo,
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        ViewBag.CurrentStatus = lead.Status;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadVM vm)
    {
        if (id != vm.LeadId) return BadRequest();
        if (!ModelState.IsValid)
        {
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var lead = new Lead
        {
            LeadId = vm.LeadId,
            LeadName = vm.LeadName,
            Email = vm.Email,
            Phone = vm.Phone,
            CompanyName = vm.CompanyName,
            Source = vm.Source,
            Priority = vm.Priority,
            ExpectedValue = vm.ExpectedValue,
            Notes = vm.Notes,
            AssignedTo = vm.AssignedTo
        };

        var (success, error) = await _leadService.UpdateAsync(lead, vm.Status);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _leadService.DeleteAsync(id);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Lead deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var (userId, _) = await GetUserContextAsync();
        var (success, error) = await _leadService.ConvertLeadAsync(id, userId);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Lead converted to customer successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<SelectListItem>> GetSalesExecutivesAsync()
    {
        var executives = await _userManager.GetUsersInRoleAsync(AppRoles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(AppRoles.Manager);
        return executives.Concat(managers)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FullName, u.Id)).ToList();
    }
}
