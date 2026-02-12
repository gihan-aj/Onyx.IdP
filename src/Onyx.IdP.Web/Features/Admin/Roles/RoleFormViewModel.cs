using System.ComponentModel.DataAnnotations;

namespace Onyx.IdP.Web.Features.Admin.Roles;

public class RoleFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Role Name is required")]
    [StringLength(256, ErrorMessage = "Role Name cannot exceed 256 characters")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}
