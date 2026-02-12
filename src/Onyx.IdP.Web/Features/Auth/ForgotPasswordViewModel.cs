using System.ComponentModel.DataAnnotations;

namespace Onyx.IdP.Web.Features.Auth;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
