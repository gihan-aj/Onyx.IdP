using System.ComponentModel.DataAnnotations;

namespace Onyx.IdP.Web.Features.Admin.Users;

public class UserFormViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }

    [Display(Name = "Lockout Enabled")]
    public bool LockoutEnabled { get; set; }

    public List<RoleSelectionItem> AvailableRoles { get; set; } = new();
}

public class RoleSelectionItem
{
    public string? RoleName { get; set; }
    public bool Selected { get; set; }
}
