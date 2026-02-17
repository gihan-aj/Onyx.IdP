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
        // For Client Credentials flow, the User.Identity.Name is the ClientId
        var clientId = User.Identity?.Name;
        if (string.IsNullOrEmpty(clientId))
        {
             return BadRequest("Could not determine Client ID.");
        }

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
}
