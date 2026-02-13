using Microsoft.AspNetCore.Identity;

namespace Onyx.IdP.Core.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
