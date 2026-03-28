using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Onyx.IdP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Onyx.IdP.Core.Settings;
using Microsoft.Extensions.Options;

namespace Onyx.IdP.Infrastructure.Data;

public class DataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OpenIddictClientsOptions _clientsOptions;

    public DataSeeder(IServiceProvider serviceProvider, IOptions<OpenIddictClientsOptions> openIddictClientsOptions)
    {
        _serviceProvider = serviceProvider;
        _clientsOptions = openIddictClientsOptions.Value;
    }

    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        await context.Database.MigrateAsync();

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
                LastName = "Admin",
                TenantId = Guid.Empty
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

        if (await scopeManager.FindByNameAsync("oms_api") is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "oms_api",
                DisplayName = "OMS API Access",
                Description = "Access to the Order Management System API.",
                Resources = { _clientsOptions.OmsApi.ClientId }
            });
        }

        // Seed Public Client
        if (await manager.FindByClientIdAsync(_clientsOptions.OmsClient.ClientId) is null)
        {
            var desktopAppDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = _clientsOptions.OmsClient.ClientId,
                DisplayName = _clientsOptions.OmsClient.DisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,

                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                    OpenIddictConstants.Permissions.ResponseTypes.Code,

                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "oms_api",
                }
            };

            foreach( var uri in _clientsOptions.OmsClient.RedirectUris)
            {
                desktopAppDescriptor.RedirectUris.Add(new Uri(uri));
            }

            await manager.CreateAsync(desktopAppDescriptor);
        }

        // Seed Order Management System Client (Machine-to-Machine)
        if (await manager.FindByClientIdAsync(_clientsOptions.OmsApi.ClientId) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = _clientsOptions.OmsApi.ClientId,
                ClientSecret = _clientsOptions.OmsApi.ClientSecret,
                DisplayName = _clientsOptions.OmsApi.DisplayName,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                }
            });
        }
    }
}
