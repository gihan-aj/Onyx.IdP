using Microsoft.AspNetCore.Identity;
using Onyx.IdP.Core.Entities;

namespace Onyx.IdP.Infrastructure.Extensions
{
    public static class UserManagerExtensions
    {
        public static Task<ApplicationUser?> FindByIdAsync(this UserManager<ApplicationUser> userManager, Guid userId)
        {
            return userManager.FindByIdAsync(userId.ToString());
        }
    }
}
