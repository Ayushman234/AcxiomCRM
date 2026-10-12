using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = $"{AppRoles.Admin}")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _audit;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var users = _userManager.Users.Where(u => u.IsActive || search != null).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            users = users.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email!.ToLower().Contains(s));
        }

        var userList = await users.OrderBy(u => u.FirstName).ToListAsync();
        var vmList = new List<UserListVM>();

        foreach (var u in userList)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(u);
            vmList.Add(new UserListVM
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "None",
                IsActive = u.IsActive,
                IsLockedOut = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow,
                CreatedDate = u.CreatedDate,
                LastLoginDate = u.LastLoginDate
            });
        }

        ViewBag.Search = search;
        return View(vmList);
    }

    public IActionResult Create()
    {
        var vm = new CreateUserVM();
        ViewBag.Roles = AppRoles.All;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = AppRoles.All;
            return View(vm);
        }

        if (!AppRoles.All.Contains(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), "Invalid role selected.");
            ViewBag.Roles = AppRoles.All;
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            IsActive = vm.IsActive,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            ViewBag.Roles = AppRoles.All;
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, vm.Role);
        await _audit.LogAsync("Create", "User", user.Id,
            newValue: $"{user.Email} as {vm.Role}");
        TempData["Success"] = $"User '{vm.Email}' created with role {vm.Role}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var vm = new EditUserVM
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive
        };
        ViewBag.Roles = AppRoles.All;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserVM vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = AppRoles.All;
            return View(vm);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var oldRoles = await _userManager.GetRolesAsync(user);
        var oldRole = oldRoles.FirstOrDefault();

        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;
        user.IsActive = vm.IsActive;

        await _userManager.UpdateAsync(user);

        // Role change
        if (oldRole != vm.Role)
        {
            if (oldRole != null)
                await _userManager.RemoveFromRoleAsync(user, oldRole);
            await _userManager.AddToRoleAsync(user, vm.Role);
            await _audit.LogAsync("RoleChange", "User", user.Id,
                oldValue: oldRole, newValue: vm.Role);
        }

        await _audit.LogAsync("Update", "User", user.Id, details: $"Updated user {user.Email}");
        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        await _audit.LogAsync(user.IsActive ? "Activate" : "Deactivate",
            "User", user.Id, oldValue: (!user.IsActive).ToString(), newValue: user.IsActive.ToString());
        TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _audit.LogAsync("Unlock", "User", user.Id, details: "Admin unlocked account");
        TempData["Success"] = "User account unlocked.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var vm = new ChangePasswordVM { UserId = user.Id, UserName = user.FullName };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ChangePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByIdAsync(vm.UserId);
        if (user == null) return NotFound();

        // Remove and re-add password (secure admin reset — no old password needed)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        await _audit.LogAsync("PasswordReset", "User", user.Id,
            details: "Admin reset password — new password NOT logged");
        TempData["Success"] = $"Password reset for {user.FullName}.";
        return RedirectToAction(nameof(Index));
    }
}
