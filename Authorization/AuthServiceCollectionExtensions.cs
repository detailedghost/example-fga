using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace FgaPoc.Authorization;

file static class RedirectHelpers
{
    // /api requests are called by the JS frontend, which wants a status code, not an HTML redirect.
    public static Task ApiAwareStatus(
        RedirectContext<CookieAuthenticationOptions> ctx,
        int apiStatusCode
    )
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = apiStatusCode;
        else
            ctx.Response.Redirect(ctx.RedirectUri);

        return Task.CompletedTask;
    }
}

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
                // Pages get redirected to login; /api callers (the JS frontend) get a status code.
                options.Events.OnRedirectToLogin = ctx =>
                    RedirectHelpers.ApiAwareStatus(ctx, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = ctx =>
                    RedirectHelpers.ApiAwareStatus(ctx, StatusCodes.Status403Forbidden);
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
}
