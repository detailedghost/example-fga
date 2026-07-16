using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace FgaPoc.Authorization;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Wires cookie identity plus the OpenFGA-backed authorization policies and handlers.
    /// ASP.NET decides <em>who you are</em>; every <em>what you may do</em> check defers to OpenFGA.
    /// </summary>
    public static IServiceCollection AddAppAuth(this IServiceCollection services)
    {
        services.AddSingleton<RoleResolver>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/Forbidden";
                // Pages get redirected to login; /api and /mvc callers (the JS frontend) get a status code.
                options.Events.OnRedirectToLogin = ctx =>
                    RespondForFrontend(ctx, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = ctx =>
                    RespondForFrontend(ctx, StatusCodes.Status403Forbidden);
            });

        services.AddSingleton<IAuthorizationHandler, CanCreatePostHandler>();
        services.AddSingleton<IAuthorizationHandler, CanManageAccessHandler>();
        services.AddSingleton<IAuthorizationHandler, PostOperationHandler>();

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                Policies.CanCreatePost,
                policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(new CanCreatePostRequirement())
            )
            .AddPolicy(
                Policies.CanManageAccess,
                policy =>
                    policy
                        .RequireAuthenticatedUser()
                        .AddRequirements(new CanManageAccessRequirement())
            );

        return services;
    }

    // Pages get redirected to login; /api and /mvc callers (the JS frontend) get a status code.
    private static Task RespondForFrontend(
        RedirectContext<CookieAuthenticationOptions> ctx,
        int apiStatusCode
    )
    {
        var path = ctx.Request.Path;
        if (path.StartsWithSegments("/api") || path.StartsWithSegments("/mvc"))
        {
            ctx.Response.StatusCode = apiStatusCode;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }
}
