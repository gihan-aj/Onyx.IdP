using System.ComponentModel.DataAnnotations;
using OpenIddict.Abstractions;

namespace Onyx.IdP.Web.Features.Admin.ClientApps;

public class ClientAppListViewModel
{
    public List<ClientAppDto> Clients { get; set; } = new();
}

public class ClientAppDto
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Type { get; set; }
}

public class ClientAppFormViewModel
{
    public string? Id { get; set; }

    [Required]
    [Display(Name = "Client ID")]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Client Secret")]
    public string? ClientSecret { get; set; } // Optional for edits

    [Display(Name = "Redirect URIs")]
    [Required]
    public string RedirectUris { get; set; } = string.Empty;

    [Display(Name = "Post Logout Redirect URIs")]
    public string? PostLogoutRedirectUris { get; set; }

    // Permissions (simplified for UI)
    public bool GrantAuthorizationCode { get; set; }
    public bool GrantClientCredentials { get; set; }
    public bool GrantRefreshToken { get; set; }

    // Scopes
    public List<ScopeSelectionItem> Scopes { get; set; } = new();
}

public class ScopeSelectionItem
{
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
