using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Web.Features.Admin.Roles;

[Authorize(Roles = "Admin")]
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
                Description = r.Description
            })
            .ToListAsync();

        // Get User Counts (optimization: do this separately or via group join if performance is critical, but for admin panel Loop is okay for now or separate query)
        // For simplicity, we'll fetch user counts. Note: This can be N+1 problem. 
        // Better approach: 
        // var rolesWithCounts = await _roleManager.Roles... (Roles doesn't assume navigation property to users usually in Identity unless configured)
        // Let's stick to simple query for now.
        
        foreach (var role in roles)
        {
           var identityRole = await _roleManager.FindByIdAsync(role.Id);
           if (identityRole != null)
           {
               // This allows checking users in role. 
               // However, `GetUsersInRoleAsync` returns a list, which is heavy. 
               // For now, let's skip UserCount or do it efficiently if possible. 
               // Identity doesn't expose `Users` navigation on Role by default in all templates.
               // Let's leave UserCount as 0 for this step to avoid performance hit or complex query, or implemented later.
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
            Description = model.Description
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
        if (role == null)
        {
            return NotFound();
        }

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
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
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
}
