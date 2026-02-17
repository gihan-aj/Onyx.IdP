using System.ComponentModel.DataAnnotations;

namespace Onyx.IdP.Web.Features.Api;

public class CreateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = string.Empty;
}
