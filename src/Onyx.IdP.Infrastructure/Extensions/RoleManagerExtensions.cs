using Microsoft.AspNetCore.Identity;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Infrastructure.Extensions
{
    public static class RoleManagerExtensions
    {
        public static Task<ApplicationRole?> FindByIdAsync(this RoleManager<ApplicationRole> roleManager, Guid roleName)
        {
            return roleManager.FindByIdAsync(roleName.ToString());
        }
    }
}
