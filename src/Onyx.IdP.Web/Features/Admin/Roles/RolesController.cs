using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Web.Features.Admin.Roles;

[Authorize(Roles = "SuperAdmin,Admin")]
[Route("Admin/[controller]")]
public class RolesController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? sortColumn = "Name",
        string? sortDirection = "asc",
        int page = 1,
        int pageSize = 10)
    {
        var query = _roleManager.Roles.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.Name!.Contains(searchTerm) || (r.Description != null && r.Description.Contains(searchTerm)));
        }

        // Sort
        query = sortColumn switch
        {
            "Name" => sortDirection == "asc" ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
            "Description" => sortDirection == "asc" ? query.OrderBy(r => r.Description) : query.OrderByDescending(r => r.Description),
            _ => query.OrderBy(r => r.Name)
        };

        // Pagination
        var totalCount = await query.CountAsync();
        var roles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsActive = r.IsActive
            })
            .ToListAsync();

        foreach (var role in roles)
        {
           var identityRole = await _roleManager.FindByIdAsync(role.Id);
           if (identityRole != null)
           {
               role.UserCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
           }
        }

        var viewModel = new RolesViewModel
        {
            Roles = roles,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchTerm = searchTerm,
            SortColumn = sortColumn,
            SortDirection = sortDirection
        };

        return View(viewModel);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new RoleFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _roleManager.CreateAsync(new ApplicationRole
        {
            Name = model.Name,
            Description = model.Description,
            IsActive = true
        });

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

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        return View(new RoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description
        });
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RoleFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        // Protect SuperAdmin name change
        if (role.Name == "SuperAdmin" && model.Name != "SuperAdmin")
        {
             ModelState.AddModelError(string.Empty, "Cannot change the name of the SuperAdmin role.");
             return View(model);
        }
        
        // Protect User name change
        if (role.Name == "User" && model.Name != "User")
        {
             ModelState.AddModelError(string.Empty, "Cannot change the name of the User role.");
             return View(model);
        }

        role.Name = model.Name;
        role.Description = model.Description;

        var result = await _roleManager.UpdateAsync(role);

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
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        // Prevent deactivating protected roles
        if (role.Name == "SuperAdmin" || role.Name == "User")
        {
            TempData["Error"] = $"Cannot deactivate {role.Name} role.";
            return RedirectToAction(nameof(Index));
        }

        role.IsActive = !role.IsActive;
        await _roleManager.UpdateAsync(role);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        if (role.Name == "SuperAdmin" || role.Name == "User")
        {
            TempData["Error"] = $"Cannot delete {role.Name} role.";
            return RedirectToAction(nameof(Index));
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        return View(new DeleteRoleViewModel
        {
            Id = role.Id,
            Name = role.Name!,
            UserCount = usersInRole.Count
        });
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, DeleteRoleViewModel model)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        if (role.Name == "SuperAdmin" || role.Name == "User")
        {
             TempData["Error"] = $"Cannot delete {role.Name} role.";
             return RedirectToAction(nameof(Index));
        }

        if (model.UnassignUsers)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            foreach (var user in usersInRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name!);
            }
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
             ModelState.AddModelError(string.Empty, error.Description);
        }

        var currentUsers = await _userManager.GetUsersInRoleAsync(role.Name!);
        model.UserCount = currentUsers.Count;
        model.Name = role.Name!;

        return View(model);
    }
}
