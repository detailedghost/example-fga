using System.Security.Claims;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Endpoints;

/// <summary>Login/logout — cookie identity only. Authorization is decided later by the selected provider.</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/login",
                async (
                    [FromForm] string username,
                    [FromForm] string password,
                    HttpContext http,
                    UserRepository users
                ) =>
                {
                    var user = await users.GetByUsernameAsync(username);
                    if (user is null || user.Password != password)
                        return Results.Redirect("/Login?error=1");

                    // The Name claim becomes the authorization principal; display name is for the UI.
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, user.Username),
                        new("display_name", user.DisplayName),
                    };
                    var identity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    );
                    await http.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity)
                    );
                    return Results.Redirect("/");
                }
            )
            .DisableAntiforgery();

        app.MapPost(
                "/logout",
                async (HttpContext http) =>
                {
                    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/Login");
                }
            )
            .DisableAntiforgery();
    }
}
