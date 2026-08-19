using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Controllers;

/// <summary>
/// MVC-controller version of the role-matrix JSON API (admin only). Mirrors the minimal-API
/// version in Endpoints/AccessEndpoints.cs (<c>/api/access</c>) so both styles sit side by side.
/// </summary>
[ApiController]
[Route("mvc/access")]
[Authorize(Policy = Policies.CanManageAccess)]
public sealed class AccessController(UserRepository users, IPermissionService permissions)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMatrix(CancellationToken ct)
    {
        var directRolesByUser = (await permissions.ReadRoleAssignmentsAsync(ct))
            .GroupBy(a => a.Username)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Role).ToArray());

        var all = await users.GetAllAsync(ct);
        return Ok(
            new
            {
                currentUser = User.Identity?.Name,
                provider = permissions.ProviderId,
                roles = BlogAuthorizationModel.Roles,
                users = all.Select(u => new
                {
                    username = u.Username,
                    displayName = u.DisplayName,
                    roles = directRolesByUser.GetValueOrDefault(u.Username, []),
                }),
            }
        );
    }

    [HttpPost("grant")]
    public async Task<IActionResult> Grant(
        [FromForm] string username,
        [FromForm] string role,
        CancellationToken ct
    )
    {
        await permissions.GrantRoleAsync(username, role, ct);
        return NoContent();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        [FromForm] string username,
        [FromForm] string role,
        CancellationToken ct
    )
    {
        await permissions.RevokeRoleAsync(username, role, ct);
        return NoContent();
    }
}
