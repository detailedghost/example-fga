using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Endpoints;

/// <summary>
/// Minimal-API version of the role-matrix JSON API (admin only). The same surface is also
/// implemented as an MVC controller in Controllers/AccessController.cs — kept side by side
/// as a reference for both styles.
/// </summary>
public static class AccessEndpoints
{
    public static void MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/access")
            .RequireAuthorization(Policies.CanManageAccess)
            .DisableAntiforgery();

        group.MapGet(
            "",
            async (UserRepository users, IPermissionService permissions, HttpContext http) =>
            {
                var directRolesByUser = (await permissions.ReadRoleAssignmentsAsync())
                    .GroupBy(a => a.Username)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Role).ToArray());

                var all = await users.GetAllAsync();
                return Results.Json(
                    new
                    {
                        currentUser = http.User.Identity?.Name,
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
        );

        // Toggled via fetch, so return 204 rather than a redirect.
        group.MapPost(
            "/grant",
            async (
                [FromForm] string username,
                [FromForm] string role,
                IPermissionService permissions
            ) =>
            {
                await permissions.GrantRoleAsync(username, role);
                return Results.NoContent();
            }
        );

        group.MapPost(
            "/revoke",
            async (
                [FromForm] string username,
                [FromForm] string role,
                IPermissionService permissions
            ) =>
            {
                await permissions.RevokeRoleAsync(username, role);
                return Results.NoContent();
            }
        );
    }
}
