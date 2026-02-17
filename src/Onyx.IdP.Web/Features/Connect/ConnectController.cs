using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Onyx.IdP.Core.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore;

namespace Onyx.IdP.Web.Features.Connect
{
    public class ConnectController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ConnectController(
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOpenIddictScopeManager scopeManager)
        {
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _scopeManager = scopeManager;
        }

        // -------------------------------------------------------------------------
        // AUTHORIZE ENDPOINT
        // -------------------------------------------------------------------------
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the user principal stored in the authentication cookie.
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            // If the user principal can't be retrieved, challenge the user to log in.
            if (!result.Succeeded || result.Principal is not ClaimsPrincipal principal)
            {
                return Challenge(
                    authenticationSchemes: IdentityConstants.ApplicationScheme,
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    });
            }

            var user = await _userManager.GetUserAsync(principal);
            if (user == null)
            {
                 var userId = _userManager.GetUserId(principal)
                        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value;

                if (userId != null) user = await _userManager.FindByIdAsync(userId);
            }

            if (user == null)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Challenge(IdentityConstants.ApplicationScheme);
            }

            // Retrieve the client application details.
            var clientId = request.ClientId ?? throw new InvalidOperationException("Client ID cannot be found.");
            var application = await _applicationManager.FindByClientIdAsync(clientId) ??
                throw new InvalidOperationException("Details concerning the calling client application cannot be found.");
            
            var applicationId = await _applicationManager.GetIdAsync(application) ??
                 throw new InvalidOperationException("Details concerning the calling client application cannot be found.");

            var userIdString = await _userManager.GetUserIdAsync(user);
            var scopes = request.GetScopes();

            // Handle Consent Logic
            if (Request.HasFormContentType)
            {
                var consentAction = Request.Form["consent_action"].ToString();
                if (consentAction == "deny")
                {
                    return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
                if (consentAction == "accept")
                {
                    await _authorizationManager.CreateAsync(
                        principal: principal,
                        subject: userIdString,
                        client: applicationId,
                        type: AuthorizationTypes.Permanent,
                        scopes: scopes);
                }
            }

            var authorizations = await _authorizationManager.FindAsync(
                subject: userIdString,
                client: applicationId,
                status: Statuses.Valid,
                type: AuthorizationTypes.Permanent,
                scopes: scopes).ToListAsync();

            var consentType = await _applicationManager.GetConsentTypeAsync(application);
            if (consentType != ConsentTypes.Implicit && !authorizations.Any())
            {
                return View("Consent", new ConsentViewModel
                {
                    ApplicationName = await _applicationManager.GetDisplayNameAsync(application) ?? "Unknown Application",
                    Scopes = scopes,
                    ScopeDescriptions = await GetScopeDescriptions(scopes),
                    ReturnUrl = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
            }

            // Create the identity ticket
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.AddClaim(Claims.Subject, userIdString);
            identity.AddClaim(Claims.Name, await _userManager.GetUserNameAsync(user) ?? user.UserName ?? "Unknown User");
            identity.AddClaim(Claims.Email, await _userManager.GetEmailAsync(user) ?? "");
            
            // Add Active Roles
            var activeRoles = await GetActiveRolesAsync(user, clientId);
            foreach (var role in activeRoles)
            {
                identity.AddClaim(Claims.Role, role);
            }

            identity.SetScopes(scopes);
            var resources = await _scopeManager.ListResourcesAsync(scopes).ToListAsync();
            identity.SetResources(resources);

            var newPrincipal = new ClaimsPrincipal(identity);
            newPrincipal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorizations.LastOrDefault()!));

            foreach (var claim in identity.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, newPrincipal));
            }

            return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // -------------------------------------------------------------------------
        // TOKEN EXCHANGE ENDPOINT
        // -------------------------------------------------------------------------
        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded || result.Principal is null)
                {
                    return Forbid(
                       authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                       properties: new AuthenticationProperties(new Dictionary<string, string?>
                       {
                           [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                           [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                       }));
                }

                var user = await _userManager.GetUserAsync(result.Principal);
                if (user == null)
                {
                    var userId = _userManager.GetUserId(result.Principal)
                            ?? result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? result.Principal.FindFirst("sub")?.Value;
                    if (userId != null) user = await _userManager.FindByIdAsync(userId);
                }

                if (user == null || !await _signInManager.CanSignInAsync(user))
                {
                    return Forbid(
                       authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                       properties: new AuthenticationProperties(new Dictionary<string, string?>
                       {
                           [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                           [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                       }));
                }

                // Note: For authorization code flow, we reuse the principal stored in the authorization code.
                // However, if we want to refresh claims (like roles) on token refresh, we should rebuild the identity.
                // For simplicity here, we rebuild it to ensure active roles are checked again.
                
                var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);
                
                var userIdString = await _userManager.GetUserIdAsync(user);
                identity.AddClaim(Claims.Subject, userIdString);
                identity.AddClaim(Claims.Name, await _userManager.GetUserNameAsync(user) ?? user.UserName ?? "Unknown User");
                identity.AddClaim(Claims.Email, await _userManager.GetEmailAsync(user) ?? "");

                // Add Active Roles
                var activeRoles = await GetActiveRolesAsync(user, request.ClientId ?? "");
                foreach (var role in activeRoles)
                {
                    identity.AddClaim(Claims.Role, role);
                }

                var grantedScopes = result.Principal.GetScopes();
                identity.SetScopes(grantedScopes);
                var resources = await _scopeManager.ListResourcesAsync(grantedScopes).ToListAsync();
                identity.SetResources(resources);

                // Copy authorizations from original principal
                var newPrincipal = new ClaimsPrincipal(identity);
                newPrincipal.SetAuthorizationId(result.Principal.GetAuthorizationId());
                newPrincipal.SetCreationDate(DateTimeOffset.UtcNow);
                newPrincipal.SetExpirationDate(result.Principal.GetExpirationDate());
                
                foreach (var claim in identity.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, newPrincipal));
                }

                return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsClientCredentialsGrantType())
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? "");
                if (application == null)
                {
                     return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application was not found."
                        }));
                }

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var clientId = await _applicationManager.GetClientIdAsync(application);
                var displayName = await _applicationManager.GetDisplayNameAsync(application);

                identity.AddClaim(Claims.Subject, clientId ?? "Unknown");
                identity.AddClaim(Claims.Name, displayName ?? clientId ?? "Unknown");
                identity.SetScopes(request.GetScopes());
                
                var resources = await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync();
                identity.SetResources(resources);

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new NotImplementedException("The specified grant type is not implemented.");
        }

        // -------------------------------------------------------------------------
        // USERINFO ENDPOINT
        // -------------------------------------------------------------------------
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo")]
        [HttpPost("~/connect/userinfo")]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                var subject = User.FindFirst(Claims.Subject)?.Value;
                if (!string.IsNullOrEmpty(subject)) user = await _userManager.FindByIdAsync(subject);
            }

            if (user is null)
            {
                return Challenge(
                   authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                   properties: new AuthenticationProperties(new Dictionary<string, string?>
                   {
                       [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                       [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                   }));
            }

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [Claims.Subject] = await _userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(Scopes.Email))
            {
                claims[Claims.Email] = await _userManager.GetEmailAsync(user) ?? "";
                claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.Name] = await _userManager.GetUserNameAsync(user) ?? "";
                claims[Claims.PreferredUsername] = await _userManager.GetUserNameAsync(user) ?? "";
            }

            if (User.HasScope(Scopes.Roles))
            {
                var clientId = User.FindFirst(Claims.ClientId)?.Value 
                               ?? User.FindFirst("client_id")?.Value 
                               ?? User.FindFirst("azp")?.Value;

                if (!string.IsNullOrEmpty(clientId))
                {
                    claims[Claims.Role] = await GetActiveRolesAsync(user, clientId);
                }
            }

            return Ok(claims);
        }

        // -------------------------------------------------------------------------
        // LOGOUT ENDPOINT
        // -------------------------------------------------------------------------
        [HttpGet("~/connect/logout")]
        [HttpPost("~/connect/logout")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties { RedirectUri = "/" });
        }

        // -------------------------------------------------------------------------
        // HELPER METHODS
        // -------------------------------------------------------------------------

        private async Task<List<string>> GetActiveRolesAsync(ApplicationUser user, string clientId)
        {
            var allRoleNames = await _userManager.GetRolesAsync(user);
            var activeRoles = new List<string>();
            var prefix = $"{clientId}_";

            foreach (var roleName in allRoleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null && role.IsActive)
                {
                    if (roleName.StartsWith(prefix))
                    {
                        activeRoles.Add(roleName.Substring(prefix.Length));
                    }
                }
            }
            return activeRoles;
        }

        private async Task<IEnumerable<string>> GetScopeDescriptions(IEnumerable<string> scopes)
        {
            var descriptions = new List<string>();
            foreach (var scope in scopes)
            {
                var scopeEntity = await _scopeManager.FindByNameAsync(scope);
                if (scopeEntity != null)
                {
                    var description = await _scopeManager.GetDescriptionAsync(scopeEntity);
                    var displayName = await _scopeManager.GetDisplayNameAsync(scopeEntity);
                    descriptions.Add(description ?? displayName ?? $"Access the '{scope}' scope");
                    continue;
                }

                var standardDescription = scope switch
                {
                    OpenIddictConstants.Scopes.Email => "View your email address",
                    OpenIddictConstants.Scopes.Profile => "View your basic profile details (name, username)",
                    OpenIddictConstants.Scopes.Roles => "View your assigned roles",
                    OpenIddictConstants.Scopes.OfflineAccess => "Access your data even when you are not logged in",
                    OpenIddictConstants.Scopes.OpenId => "Sign you in using your identity",
                    _ => $"Access the '{scope}' scope"
                };
                descriptions.Add(standardDescription);
            }
            return descriptions;
        }

        private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            switch (claim.Type)
            {
                case Claims.Name:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope(Scopes.Profile)) yield return Destinations.IdentityToken;
                    yield break;
                case Claims.Email:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope(Scopes.Email)) yield return Destinations.IdentityToken;
                    yield break;
                case Claims.Role:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope(Scopes.Roles)) yield return Destinations.IdentityToken;
                    yield break;
                case "permission":
                    yield return Destinations.AccessToken;
                    yield break;
                case "AspNet.Identity.SecurityStamp": yield break;
                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }
    }
}
