using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using Onyx.IdP.Web.Features.Admin.ClientApps; // Ensure namespace is correct

namespace Onyx.IdP.Web.Features.Admin.ClientApps;

[Authorize(Roles = "SuperAdmin,Admin")]
[Route("Admin/[controller]")]
public class ClientAppsController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public ClientAppsController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var clients = new List<ClientAppDto>();

        // Note: OpenIddict async enumerable
        await foreach (var app in _applicationManager.ListAsync())
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, app);

            clients.Add(new ClientAppDto
            {
                Id = await _applicationManager.GetIdAsync(app) ?? string.Empty,
                ClientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty,
                DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
                Type = await _applicationManager.GetClientTypeAsync(app)
            });
        }

        return View(new ClientAppListViewModel { Clients = clients });
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = new ClientAppFormViewModel
        {
            Scopes = await GetAvailableScopesAsync()
        };
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientAppFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Scopes = await GetAvailableScopesAsync(model.Scopes);
            return View(model);
        }

        if (await _applicationManager.FindByClientIdAsync(model.ClientId) != null)
        {
            ModelState.AddModelError("ClientId", "Client ID already exists.");
            model.Scopes = await GetAvailableScopesAsync(model.Scopes);
            return View(model);
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret, // OpenIddict handles hashing if configured
            DisplayName = model.DisplayName,
            Permissions = {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.Revocation
            }
        };

        // Redirect URIs
        if (!string.IsNullOrWhiteSpace(model.RedirectUris))
        {
            foreach (var uri in model.RedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var url))
                    descriptor.RedirectUris.Add(url);
            }
        }

        // Post Logout Redirect URIs
        if (!string.IsNullOrWhiteSpace(model.PostLogoutRedirectUris))
        {
            foreach (var uri in model.PostLogoutRedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var url))
                    descriptor.PostLogoutRedirectUris.Add(url);
            }
        }

        // Permissions
        if (model.GrantAuthorizationCode)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }
        if (model.GrantClientCredentials) descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        if (model.GrantRefreshToken) 
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess);
        }

        // Scopes
        foreach (var scope in model.Scopes.Where(s => s.Selected))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope.Name);
        }

        try 
        {
            await _applicationManager.CreateAsync(descriptor);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Error creating client: " + ex.Message);
            model.Scopes = await GetAvailableScopesAsync(model.Scopes);
            return View(model);
        }
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var app = await _applicationManager.FindByIdAsync(id);
        if (app == null) return NotFound();

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        var allScopes = await GetAvailableScopesAsync();
        // Mark selected scopes
        foreach (var scope in allScopes)
        {
            if (descriptor.Permissions.Contains(OpenIddictConstants.Permissions.Prefixes.Scope + scope.Name))
            {
                scope.Selected = true;
            }
        }

        var model = new ClientAppFormViewModel
        {
            Id = id,
            ClientId = descriptor.ClientId ?? string.Empty,
            DisplayName = descriptor.DisplayName ?? string.Empty,
            RedirectUris = string.Join("\n", descriptor.RedirectUris),
            PostLogoutRedirectUris = string.Join("\n", descriptor.PostLogoutRedirectUris),
            GrantAuthorizationCode = descriptor.Permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode),
            GrantClientCredentials = descriptor.Permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials),
            GrantRefreshToken = descriptor.Permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken),
            Scopes = allScopes
        };

        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ClientAppFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        // Skip ClientSecret validation on edit if empty
        if (string.IsNullOrWhiteSpace(model.ClientSecret))
        {
            ModelState.Remove("ClientSecret");
        }

        if (!ModelState.IsValid)
        {
            model.Scopes = await GetAvailableScopesAsync(model.Scopes);
            return View(model);
        }

        var app = await _applicationManager.FindByIdAsync(id);
        if (app == null) return NotFound();

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        descriptor.DisplayName = model.DisplayName;
        
        // Update Secret ONLY if provided
        if (!string.IsNullOrWhiteSpace(model.ClientSecret))
        {
            descriptor.ClientSecret = model.ClientSecret;
        }

        // Update Redirect URIs
        descriptor.RedirectUris.Clear();
        if (!string.IsNullOrWhiteSpace(model.RedirectUris))
        {
            foreach (var uri in model.RedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var url))
                    descriptor.RedirectUris.Add(url);
            }
        }

        // Update PostLogout Redirect URIs
        descriptor.PostLogoutRedirectUris.Clear();
        if (!string.IsNullOrWhiteSpace(model.PostLogoutRedirectUris))
        {
            foreach (var uri in model.PostLogoutRedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var url))
                    descriptor.PostLogoutRedirectUris.Add(url);
            }
        }

        // Update Permissions
        // We preserve endpoint permissions and only update grant types and scopes
        var basePermissions = descriptor.Permissions.Where(p => 
            p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Endpoint) || 
            p.StartsWith(OpenIddictConstants.Permissions.Prefixes.ResponseType)).ToList();
        
        // However, response types are tied to grant types, so simplistic approach:
        descriptor.Permissions.Clear();
        descriptor.Requirements.Clear();
        
        // Re-add required endpoints
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);

        if (model.GrantAuthorizationCode)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        }
        if (model.GrantClientCredentials) descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        if (model.GrantRefreshToken) 
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess);
        }

        // Scopes
        foreach (var scope in model.Scopes.Where(s => s.Selected))
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope.Name);
        }

        try
        {
            // Note: UpdateAsync takes the entity and descriptor. 
            // Actually, OpenIddict managers usually have UpdateAsync(entity, descriptor) or similar.
            // Let's check IOpenIddictApplicationManager signature. 
            // It's UpdateAsync(application, descriptor, token).
            // But wait, UpdateAsync(application, token) updates properties. 
            // To update form descriptor, we might need a different approach or manually update application properties.
            // Actually, PopulateAsync effectively copies to descriptor.
            // But to apply changes back, we usually do:
            await _applicationManager.UpdateAsync(app, descriptor);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Error updating client: " + ex.Message);
            model.Scopes = await GetAvailableScopesAsync(model.Scopes);
            return View(model);
        }
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var app = await _applicationManager.FindByIdAsync(id);
        if (app == null) return NotFound();

        await _applicationManager.DeleteAsync(app);
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<ScopeSelectionItem>> GetAvailableScopesAsync(List<ScopeSelectionItem>? selected = null)
    {
        var scopes = new List<ScopeSelectionItem>();
        await foreach (var scope in _scopeManager.ListAsync())
        {
            var name = await _scopeManager.GetNameAsync(scope);
            if (!string.IsNullOrEmpty(name))
            {
                scopes.Add(new ScopeSelectionItem
                {
                    Name = name,
                    Selected = selected?.Any(s => s.Name == name && s.Selected) ?? false
                });
            }
        }
        return scopes;
    }
}
