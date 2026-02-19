using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Onyx.IdP.Core.Entities;
using OpenIddict.Validation.AspNetCore;

namespace Onyx.IdP.Web.Features.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RolesController(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [HttpPost]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var clientId = GetClientIdFromUserOrRequest(request.TargetClientId);
        if (string.IsNullOrEmpty(clientId)) return BadRequest("Could not determine Client ID.");

        var prefixedRoleName = $"{clientId}_{request.Name}";

        if (await _roleManager.RoleExistsAsync(prefixedRoleName))
        {
            return Conflict(new { message = $"Role '{request.Name}' already exists." });
        }

        var result = await _roleManager.CreateAsync(new ApplicationRole
        {
            Name = prefixedRoleName,
            IsActive = true
        });

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role '{request.Name}' created successfully." });
    }

    [HttpPut("{name}/activate")]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> ActivateRole(string name, [FromBody] RoleStatusRequest request)
    {
        var clientId = GetClientIdFromUserOrRequest(request.TargetClientId);
        if (string.IsNullOrEmpty(clientId)) return BadRequest("Could not determine Client ID.");

        var prefixedRoleName = $"{clientId}_{name}";
        var role = await _roleManager.FindByNameAsync(prefixedRoleName);

        if (role == null)
        {
            return NotFound(new { message = $"Role '{name}' not found." });
        }

        role.IsActive = true;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role '{name}' activated successfully." });
    }

    [HttpPut("{name}/deactivate")]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> DeactivateRole(string name, [FromBody] RoleStatusRequest request)
    {
        var clientId = GetClientIdFromUserOrRequest(request.TargetClientId);
        if (string.IsNullOrEmpty(clientId)) return BadRequest("Could not determine Client ID.");

        var prefixedRoleName = $"{clientId}_{name}";
        var role = await _roleManager.FindByNameAsync(prefixedRoleName);

        if (role == null)
        {
            return NotFound(new { message = $"Role '{name}' not found." });
        }

        role.IsActive = false;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role '{name}' deactivated successfully." });
    }

    [HttpPut("{name}/name")]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> UpdateRoleName(string name, [FromBody] UpdateRoleNameRequest request)
    {
        var clientId = GetClientIdFromUserOrRequest(request.TargetClientId);
        if (string.IsNullOrEmpty(clientId)) return BadRequest("Could not determine Client ID.");

        var currentPrefixedName = $"{clientId}_{name}";
        var role = await _roleManager.FindByNameAsync(currentPrefixedName);

        if (role == null)
        {
            return NotFound(new { message = $"Role '{name}' not found." });
        }

        var newPrefixedName = $"{clientId}_{request.NewName}";
        
        // Check if the new name already exists (and it's not the same role)
        if (await _roleManager.RoleExistsAsync(newPrefixedName) && newPrefixedName != currentPrefixedName)
        {
            return Conflict(new { message = $"Role '{request.NewName}' already exists." });
        }

        role.Name = newPrefixedName;
        // Identity framework handles NormalizedName update automatically when using UpdateAsync, 
        // but explicit setting is safer if we were manipulating the store directly. 
        // _roleManager.UpdateAsync is sufficient.
        
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role renamed to '{request.NewName}' successfully." });
    }

    [HttpDelete("{name}")]
    [Authorize(Policy = "RoleManagementPolicy")]
    public async Task<IActionResult> DeleteRole(string name, [FromQuery] string? targetClientId)
    {
        var clientId = GetClientIdFromUserOrRequest(targetClientId);
        if (string.IsNullOrEmpty(clientId)) return BadRequest("Could not determine Client ID.");

        var prefixedRoleName = $"{clientId}_{name}";
        var role = await _roleManager.FindByNameAsync(prefixedRoleName);

        if (role == null)
        {
            return NotFound(new { message = $"Role '{name}' not found." });
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = $"Role '{name}' deleted successfully." });
    }

    private string? GetClientIdFromUserOrRequest(string? targetClientId)
    {
        // For Client Credentials flow, the User.Identity.Name is often null.
        // We need to check the 'sub' or 'client_id' claim.
        var clientId = User.FindFirst("client_id")?.Value 
                       ?? User.FindFirst("sub")?.Value 
                       ?? User.Identity?.Name;

        // If a TargetClientId is provided, use that instead (assuming the caller is authorized to do so)
        if (!string.IsNullOrEmpty(targetClientId))
        {
            // In a real scenario, you might want to validate that 'clientId' (e.g. onyx-oms)
            // is allowed to manage 'request.TargetClientId' (e.g. order-system).
            // For now, we trust the 'idp_roles_manage' scope holder.
            return targetClientId;
        }

        return clientId;
    }
}
