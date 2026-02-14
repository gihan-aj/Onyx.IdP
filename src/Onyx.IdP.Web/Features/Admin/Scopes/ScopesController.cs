using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Onyx.IdP.Web.Features.Admin.Scopes;

[Authorize(Roles = "SuperAdmin,Admin")]
public class ScopesController : Controller
{
    private readonly IOpenIddictScopeManager _scopeManager;

    public ScopesController(IOpenIddictScopeManager scopeManager)
    {
        _scopeManager = scopeManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? sortColumn, string? sortDirection, int page = 1)
    {
        var pageSize = 10;
        var scopes = new List<ScopeDto>();

        // OpenIddict doesn't implicitly support complex querying via IOpenIddictScopeManager for all stores.
        // We will fetch all (or list) and do in-memory filtering for this simplified admin.
        // For production with massive scopes, we'd cast to the specific store or use IQueryable if available.
        // Here we assume a manageable number of scopes.
        
        var allScopes = new List<object>();
        
        // IAsyncEnumerable
        await foreach (var scope in _scopeManager.ListAsync())
        {
            allScopes.Add(scope);
        }

        var scopeDtos = new List<ScopeDto>();
        foreach (var scope in allScopes)
        {
            var descriptor = new OpenIddictScopeDescriptor();
            await _scopeManager.PopulateAsync(descriptor, scope);
            
            scopeDtos.Add(new ScopeDto
            {
                Id = await _scopeManager.GetIdAsync(scope) ?? "",
                Name = await _scopeManager.GetNameAsync(scope) ?? "",
                DisplayName = await _scopeManager.GetDisplayNameAsync(scope),
                Description = await _scopeManager.GetDescriptionAsync(scope),
                Resources = string.Join(" ", await _scopeManager.GetResourcesAsync(scope))
            });
        }

        // Filtering
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            scopeDtos = scopeDtos.Where(s => 
                s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                (s.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Sorting
        sortColumn ??= "Name";
        sortDirection ??= "asc";

        scopeDtos = sortColumn switch
        {
            "Name" => sortDirection == "asc" ? scopeDtos.OrderBy(s => s.Name).ToList() : scopeDtos.OrderByDescending(s => s.Name).ToList(),
            "DisplayName" => sortDirection == "asc" ? scopeDtos.OrderBy(s => s.DisplayName).ToList() : scopeDtos.OrderByDescending(s => s.DisplayName).ToList(),
            _ => scopeDtos.OrderBy(s => s.Name).ToList()
        };

        // Pagination
        var totalCount = scopeDtos.Count;
        var pagedScopes = scopeDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var model = new ScopesViewModel
        {
            Scopes = pagedScopes,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchTerm = searchTerm,
            SortColumn = sortColumn,
            SortDirection = sortDirection
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateScopeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScopeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _scopeManager.FindByNameAsync(model.Name) != null)
        {
            ModelState.AddModelError("Name", "A scope with this name already exists.");
            return View(model);
        }

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = model.Name,
            DisplayName = model.DisplayName,
            Description = model.Description
        };

        if (!string.IsNullOrWhiteSpace(model.Resources))
        {
            foreach (var resource in model.Resources.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                descriptor.Resources.Add(resource);
            }
        }

        await _scopeManager.CreateAsync(descriptor);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var scope = await _scopeManager.FindByIdAsync(id);
        if (scope != null)
        {
            await _scopeManager.DeleteAsync(scope);
        }
        return RedirectToAction(nameof(Index));
    }
}
