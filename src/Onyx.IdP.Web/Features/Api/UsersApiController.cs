using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Onyx.IdP.Core.Entities;
using Onyx.IdP.Core.Interfaces;
using Onyx.IdP.Web.Features.Shared;
using Onyx.IdP.Web.Services;
using OpenIddict.Validation.AspNetCore;

namespace Onyx.IdP.Web.Features.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly IRazorViewToStringRenderer _razorRenderer;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailSender emailSender,
        IRazorViewToStringRenderer razorRenderer)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _razorRenderer = razorRenderer;
    }

    [HttpGet]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = $"User with email '{email}' not found." });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> InviteUser([FromBody] CreateUserRequest request)
    {
        // For Client Credentials flow, the User.Identity.Name is often null.
        // We need to check the 'sub' or 'client_id' claim.
        var clientId = User.FindFirst("client_id")?.Value 
                       ?? User.FindFirst("sub")?.Value 
                       ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(clientId))
        {
             return BadRequest("Could not determine Client ID.");
        }

        // If a TargetClientId is provided, use that instead (assuming the caller is authorized to do so)
        if (!string.IsNullOrEmpty(request.TargetClientId))
        {
            // In a real scenario, you might want to validate that 'clientId' (e.g. onyx-oms)
            // is allowed to manage 'request.TargetClientId' (e.g. order-system).
            // For now, we trust the 'idp_roles_manage' scope holder.
            clientId = request.TargetClientId;
        }

        var prefixedRoleName = $"{clientId}_{request.RoleName}";

        // 1. Check if role exists
        if (!await _roleManager.RoleExistsAsync(prefixedRoleName))
        {
            return BadRequest(new { message = $"Role '{request.RoleName}' does not exist." });
        }

        // 2. Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = $"User with email '{request.Email}' already exists. Use the role assignment endpoint instead." });
        }

        // 3. Create User
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            EmailConfirmed = false // Will be confirmed when they click the link
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // 4. Assign Role
        await _userManager.AddToRoleAsync(user, prefixedRoleName);

        // 5. Generate Password Reset Token (as Invitation Token)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Construct Callback URL (pointing to the Auth/ResetPassword page)
        // Note: In a real scenario, you'd want a dedicated "Accept Invitation" page, 
        // but Reset Password works functionally for setting the initial password.
        var callbackUrl = Url.Action(
            "ResetPassword", 
            "Auth", 
            new { token, email = user.Email }, 
            protocol: Request.Scheme);

        // 6. Send Email
        var emailModel = new UserInvitationViewModel
        {
            Email = user.Email!,
            CallbackUrl = callbackUrl!
        };

        var emailBody = await _razorRenderer.RenderViewToStringAsync("/Views/Shared/EmailTemplates/UserInvitationTemplate.cshtml", emailModel);
        await _emailSender.SendEmailAsync(user.Email!, "You're invited to Onyx Identity", emailBody);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            IsActive = user.IsActive
        });
    }

    [HttpPost("{userId}/roles")]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        // For Client Credentials flow, the User.Identity.Name is often null.
        // We need to check the 'sub' or 'client_id' claim.
        var clientId = User.FindFirst("client_id")?.Value 
                       ?? User.FindFirst("sub")?.Value 
                       ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(clientId))
        {
             return BadRequest("Could not determine Client ID.");
        }

        // If a TargetClientId is provided, use that instead (assuming the caller is authorized to do so)
        if (!string.IsNullOrEmpty(request.TargetClientId))
        {
            // In a real scenario, you might want to validate that 'clientId' (e.g. onyx-oms)
            // is allowed to manage 'request.TargetClientId' (e.g. order-system).
            // For now, we trust the 'idp_roles_manage' scope holder.
            clientId = request.TargetClientId;
        }

        var prefixedRoleName = $"{clientId}_{request.RoleName}";

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!await _roleManager.RoleExistsAsync(prefixedRoleName))
        {
            return BadRequest(new { message = $"Role '{request.RoleName}' does not exist." });
        }

        if (await _userManager.IsInRoleAsync(user, prefixedRoleName))
        {
            return Ok(new { message = $"User is already in role '{request.RoleName}'." });
        }

        var result = await _userManager.AddToRoleAsync(user, prefixedRoleName);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role '{request.RoleName}' assigned successfully." });
    }
}
