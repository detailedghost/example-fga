using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Fga;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Endpoints;

/// <summary>Admin-only role management — grant/revoke blog roles by writing/deleting FGA tuples.</summary>
public static class AccessEndpoints
{
    public static void MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/access")
            .RequireAuthorization(Policies.CanManageAccess)
            .DisableAntiforgery();

        // JSON view of the role matrix — consumed by the framework-free /access.html frontend.
        app.MapGet(
                "/api/access",
                async (UserRepository users, FgaService fga, HttpContext http) =>
                {
                    var directRolesByUser = (await fga.ReadRoleAssignmentsAsync())
                        .GroupBy(a => a.Username)
                        .ToDictionary(g => g.Key, g => g.Select(a => a.Role).ToArray());

                    var all = await users.GetAllAsync();
                    return Results.Json(
                        new
                        {
                            currentUser = http.User.Identity?.Name,
                            roles = FgaService.Roles,
                            users = all.Select(u => new
                            {
                                username = u.Username,
                                displayName = u.DisplayName,
                                roles = directRolesByUser.GetValueOrDefault(u.Username, []),
                            }),
                        }
                    );
                }
            )
            .RequireAuthorization(Policies.CanManageAccess);

        // The access matrix toggles these via fetch, so return 204 rather than a redirect.
        group.MapPost(
            "/grant",
            async ([FromForm] string username, [FromForm] string role, FgaService fga) =>
            {
                await fga.GrantRoleAsync(username, role);
                return Results.NoContent();
            }
        );

        group.MapPost(
            "/revoke",
            async ([FromForm] string username, [FromForm] string role, FgaService fga) =>
            {
                await fga.RevokeRoleAsync(username, role);
                return Results.NoContent();
            }
        );
    }
}
