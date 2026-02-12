using System.ComponentModel.DataAnnotations;

namespace Onyx.IdP.Web.Features.Profile;

public class ProfileViewModel
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    public string? StatusMessage { get; set; }
}
