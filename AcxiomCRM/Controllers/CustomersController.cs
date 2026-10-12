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
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public CustomersController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _customerService = customerService;
        _userManager = userManager;
        _db = db;
    }

    private async Task<(string userId, string role)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    // ── Index ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(CustomerFilterVM filter)
    {
        var (userId, role) = await GetUserContextAsync();
        var (items, total) = await _customerService.GetPagedAsync(filter, userId, role);
        ViewBag.Filter = filter;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / (filter.PageSize < 1 ? 10 : filter.PageSize));
        ViewBag.TotalCount = total;
        return View(items);
    }

    // ── Details ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            customer.AssignedTo != userId && customer.CreatedBy != userId)
            return Forbid();

        return View(customer);
    }

    // ── Create GET ─────────────────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        var vm = new CustomerVM
        {
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    // ── Create POST ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var customer = new Customer
        {
            CustomerName = vm.CustomerName,
            Email = vm.Email,
            Phone = vm.Phone,
            CompanyName = vm.CompanyName,
            Address = vm.Address,
            City = vm.City,
            State = vm.State,
            Status = vm.Status,
            Notes = vm.Notes,
            AssignedTo = vm.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _customerService.CreateAsync(customer);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = $"Customer '{vm.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit GET ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            customer.AssignedTo != userId && customer.CreatedBy != userId)
            return Forbid();

        var vm = new CustomerVM
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            AssignedTo = customer.AssignedTo,
            SalesExecutives = await GetSalesExecutivesAsync()
        };
        return View(vm);
    }

    // ── Edit POST ────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerVM vm)
    {
        if (id != vm.CustomerId) return BadRequest();
        if (!ModelState.IsValid)
        {
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound();

        var (userId, role) = await GetUserContextAsync();
        if (role == AppRoles.SalesExecutive &&
            customer.AssignedTo != userId && customer.CreatedBy != userId)
            return Forbid();

        customer.CustomerName = vm.CustomerName;
        customer.Email = vm.Email;
        customer.Phone = vm.Phone;
        customer.CompanyName = vm.CompanyName;
        customer.Address = vm.Address;
        customer.City = vm.City;
        customer.State = vm.State;
        customer.Status = vm.Status;
        customer.Notes = vm.Notes;
        customer.AssignedTo = vm.AssignedTo;

        var (success, error) = await _customerService.UpdateAsync(customer);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.SalesExecutives = await GetSalesExecutivesAsync();
            return View(vm);
        }

        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete POST ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteAsync(id);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "Customer deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetSalesExecutivesAsync()
    {
        var executives = await _userManager.GetUsersInRoleAsync(AppRoles.SalesExecutive);
        var managers = await _userManager.GetUsersInRoleAsync(AppRoles.Manager);
        var all = executives.Concat(managers).OrderBy(u => u.FirstName).ToList();
        return all.Select(u => new SelectListItem(u.FullName, u.Id)).ToList();
    }
}
