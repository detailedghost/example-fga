using FgaPoc.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Controllers;

/// <summary>
/// MVC-controller version of the current-user capabilities API. Mirrors the minimal-API
/// version in Endpoints/MeEndpoints.cs (<c>/api/me</c>); the frontend actions bar uses this one.
/// </summary>
[ApiController]
[Route("mvc/me")]
[Authorize]
public sealed class MeController(IPermissionService permissions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var username = User.Identity?.Name;
        if (username is null)
            return Unauthorized();

        return Ok(
            new
            {
                user = username,
                provider = permissions.ProviderId,
                permissions = await permissions.GetPermissionsAsync(username, ct),
            }
        );
    }
}
