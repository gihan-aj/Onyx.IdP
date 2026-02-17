using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Onyx.IdP.Core.Entities;
using Onyx.IdP.Infrastructure.Data;

namespace Onyx.IdP.Infrastructure.Data;

public class DataSeeder
{
    private readonly IServiceProvider _serviceProvider;

    public DataSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        await context.Database.EnsureCreatedAsync();

        // Seed Roles
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = "SuperAdmin", Description = "System Administrator", IsActive = true });
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = "User", Description = "Standard user role", IsActive = true });
        }

        // Seed Admin User
        var adminEmail = "admin@onyx.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin"
            };
            await userManager.CreateAsync(user, "Admin123!");
            await userManager.AddToRoleAsync(user, "SuperAdmin");
        }

        // Seed Scopes
        if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Email) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = OpenIddictConstants.Scopes.Email,
                DisplayName = "Email Access",
                Description = "Access to your email address."
            });
        }

        if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Profile) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = OpenIddictConstants.Scopes.Profile,
                DisplayName = "Profile Access",
                Description = "Access to your profile details."
            });
        }

        if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Roles) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = OpenIddictConstants.Scopes.Roles,
                DisplayName = "Role Access",
                Description = "Access to your roles."
            });
        }

        if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.OfflineAccess) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = OpenIddictConstants.Scopes.OfflineAccess,
                DisplayName = "Offline Access",
                Description = "Access to your data when you are offline."
            });
        }

        if (await scopeManager.FindByNameAsync("api") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api",
                DisplayName = "API Access",
                Description = "Access to the API."
            });
        }

        if (await scopeManager.FindByNameAsync("ims_resource_server") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "ims_resource_server",
                DisplayName = "IMS API",
                Description = "Access the Inventory management system.",
                Resources = { "ims_backend_api" }
            });
        }

        if (await scopeManager.FindByNameAsync("invoice_api") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "invoice_api",
                DisplayName = "Invoice API",
                Description = "Access the Invoice management system.",
                Resources = { "invoice_backend_api" }
            });
        }

        // Seed Postman Client
        if (await manager.FindByClientIdAsync("postman") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "postman",
                ClientSecret = "postman-secret",
                DisplayName = "Postman",
                RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "ims_resource_server",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "invoice_api"
                }
            });
        }

        // Seed Order Management System Client (Machine-to-Machine)
        if (await manager.FindByClientIdAsync("order-system") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "order-system",
                ClientSecret = "order-system-secret",
                DisplayName = "Order Management System",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "idp_roles_manage"
                }
            });
        }

        // Seed Custom Scope for Role Management
        if (await scopeManager.FindByNameAsync("idp_roles_manage") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "idp_roles_manage",
                DisplayName = "Role Management",
                Description = "Manage roles and user assignments."
            });
        }
    }
}
