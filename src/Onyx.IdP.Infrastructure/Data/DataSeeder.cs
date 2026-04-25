using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Onyx.IdP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Onyx.IdP.Core.Settings;
using Microsoft.Extensions.Options;
using Onyx.IdP.Core.Constants;

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
        if (!await roleManager.RoleExistsAsync(Roles.Idp.Admin))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = Roles.Idp.Admin, Description = "Identity Provider Administrator", IsActive = true });
        }
        if (!await roleManager.RoleExistsAsync(Roles.Idp.StandardUser))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = Roles.Idp.StandardUser, Description = "Standard authenticated user", IsActive = true });
        }

        // Seed Admin User
        var adminEmail = Core.Constants.Users.Idp.Admin;
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var user = new ApplicationUser
            {
                Id = KnownIds.PlatformAdminUserId,
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                TenantId = KnownIds.HostTenantId
            };
            await userManager.CreateAsync(user, "Admin123!");
            await userManager.AddToRoleAsync(user, Roles.Idp.Admin);
        }

        // Seed Scopes
        var scopesToCreate = new[]
        {
            (OpenIddictConstants.Scopes.Email, "Email Access", "Access to your email address."),
            (OpenIddictConstants.Scopes.Profile, "Profile Access", "Access to your profile details."),
            (OpenIddictConstants.Scopes.Roles, "Role Access", "Access to your roles."),
            (OpenIddictConstants.Scopes.OfflineAccess, "Offline Access", "Access to your data when you are offline.")
        };

        foreach(var (name, display, desc) in scopesToCreate)
        {
            if(await scopeManager.FindByNameAsync(name) is null)
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = name, DisplayName = display, Description = desc });
        }

        if (await scopeManager.FindByNameAsync(AuthScopes.OmsApi) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = AuthScopes.OmsApi,
                DisplayName = "OMS API Access",
                Description = "Access to the Order Management System API.",
                Resources = { _clientsOptions.OmsApi.ClientId }
            });
        }

        if (await scopeManager.FindByNameAsync(AuthScopes.IdpApi) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = AuthScopes.IdpApi,
                DisplayName = "IdP Internal API",
                Description = "Allows backend services to manage users in the IdP."
            });
        }

        // Seed Public Client
        var existingOmsClient = await manager.FindByClientIdAsync(_clientsOptions.OmsClient.ClientId);
        if (existingOmsClient is null)
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
                    OpenIddictConstants.Permissions.Prefixes.Scope + AuthScopes.OmsApi,
                }
            };

            foreach( var uri in _clientsOptions.OmsClient.RedirectUris)
            {
                desktopAppDescriptor.RedirectUris.Add(new Uri(uri));
            }

            await manager.CreateAsync(desktopAppDescriptor);
        }

        if (existingOmsClient != null)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, existingOmsClient);

            foreach (var uri in _clientsOptions.OmsClient.RedirectUris)
            {
                var formattedUri = new Uri(uri);
                if (!descriptor.RedirectUris.Contains(formattedUri))
                {
                    descriptor.RedirectUris.Add(formattedUri);
                }
            }

            await manager.UpdateAsync(existingOmsClient, descriptor);
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

                    OpenIddictConstants.Permissions.Prefixes.Scope + AuthScopes.IdpApi
                }
            });
        }
    }
}
