using FgaPoc.Fga;

namespace FgaPoc.Endpoints;

/// <summary>Exposes the signed-in user's own capabilities so the frontend can gate its UI.</summary>
public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/me",
                async (HttpContext http, FgaService fga) =>
                {
                    var username = http.User.Identity?.Name;
                    if (username is null)
                        return Results.Unauthorized();

                    return Results.Json(
                        new
                        {
                            user = username,
                            permissions = await fga.GetPermissionsAsync(username),
                        }
                    );
                }
            )
            .RequireAuthorization();
    }
}
