using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Web.Features.Admin.Users;

[Authorize(Roles = "SuperAdmin")]
[Route("Admin/[controller]")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? searchTerm, string? sortColumn = "Email", string? sortDirection = "asc", int page = 1, int pageSize = 10)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u => u.Email!.ToLower().Contains(searchTerm) || 
                                     (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                                     (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)));
        }

        switch (sortColumn?.ToLower())
        {
            case "name":
                query = sortDirection == "desc" ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
                break;
            case "email":
            default:
                query = sortDirection == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                break;
        }

        var totalCount = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                Roles = roles
            });
        }

        var viewModel = new UsersViewModel
        {
            Users = userDtos,
            SearchTerm = searchTerm,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();

        var model = new UserFormViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            AvailableRoles = allRoles.Select(r => new RoleSelectionItem
            {
                RoleName = r.Name,
                Selected = userRoles.Contains(r.Name!)
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Email = model.Email;
        user.UserName = model.Email; // Keep UserName in sync with Email
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.EmailConfirmed = model.EmailConfirmed;
        user.LockoutEnabled = model.LockoutEnabled;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            // Update Roles
            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.AvailableRoles.Where(r => r.Selected).Select(r => r.RoleName!).ToList();

            var rolesToAdd = selectedRoles.Except(userRoles);
            var rolesToRemove = userRoles.Except(selectedRoles);

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(new DeleteUserViewModel
        {
            Id = user.Id,
            Email = user.Email!
        });
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, DeleteUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent deleting self
        if (User.Identity?.Name == user.UserName)
        {
            ModelState.AddModelError(string.Empty, "You cannot delete your own account.");
            return View(model);
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost("ToggleActive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent deactivating self
        if (User.Identity?.Name == user.UserName)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        if (!user.IsActive)
        {
            // Force logout by updating security stamp
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleLock/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent locking self
        if (User.Identity?.Name == user.UserName)
        {
            TempData["Error"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            // Unlock
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            // Lock
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }

        return RedirectToAction(nameof(Index));
    }
}
