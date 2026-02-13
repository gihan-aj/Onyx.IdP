using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Infrastructure.Services;

public class ApplicationClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public ApplicationClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        // 1. Let the base class do the heavy lifting (get user claims, etc.)
        var identity = await base.GenerateClaimsAsync(user);

        // 2. The base class added ALL roles. We need to remove the inactive ones.
        //    First, find which roles are actually assigned to this user.
        var userRoleNames = await UserManager.GetRolesAsync(user);
        
        // 3. Fetch the Role Entities to check 'IsActive' status
        //    (Optimized query to only get necessary data)
        var activeRoles = await RoleManager.Roles
            .Where(r => userRoleNames.Contains(r.Name!) && r.IsActive)
            .Select(r => r.Name)
            .ToListAsync();

        // 4. Remove ALL role claims added by the base class
        var existingRoleClaims = identity.FindAll(Options.ClaimsIdentity.RoleClaimType).ToList();
        foreach (var claim in existingRoleClaims)
        {
            identity.RemoveClaim(claim);
        }

        // 5. Re-add ONLY the active roles
        foreach (var roleName in activeRoles)
        {
            if (roleName != null)
            {
                identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName));
            }
        }

        return identity;
    }
}
