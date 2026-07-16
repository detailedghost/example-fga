using FgaPoc.Fga;
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
public sealed class MeController(FgaService fga) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var username = User.Identity?.Name;
        if (username is null)
            return Unauthorized();

        return Ok(
            new { user = username, permissions = await fga.GetPermissionsAsync(username, ct) }
        );
    }
}
