using FgaPoc.Authorization;
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
