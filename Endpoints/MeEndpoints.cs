using FgaPoc.Authorization;

namespace FgaPoc.Endpoints;

/// <summary>Exposes the signed-in user's own capabilities so the frontend can gate its UI.</summary>
public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/me",
                async (HttpContext http, IPermissionService permissions) =>
                {
                    var username = http.User.Identity?.Name;
                    if (username is null)
                        return Results.Unauthorized();

                    return Results.Json(
                        new
                        {
                            user = username,
                            provider = permissions.ProviderId,
                            permissions = await permissions.GetPermissionsAsync(username),
                        }
                    );
                }
            )
            .RequireAuthorization();
    }
}
