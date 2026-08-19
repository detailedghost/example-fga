using DotNetEnv;
using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Endpoints;
using FgaPoc.Options;

// Load .env into the process environment before configuration is built.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var config = builder.Configuration;

// All settings come from environment variables (see .env) — no appsettings.json.
var authorizationProvider = AuthorizationProviderOptions.FromConfiguration(config);
builder.Services.AddSingleton(
    new BlogDbOptions
    {
        ConnectionString =
            config["BLOG_DB_CONNECTION"]
            ?? throw new InvalidOperationException("BLOG_DB_CONNECTION is required"),
    }
);

builder.Services.AddBlogData();
builder.Services.AddPermissionProvider(config, authorizationProvider);
builder.Services.AddAppAuth();

builder.Services.AddControllers(); // MVC controllers under /mvc/* (alongside the minimal APIs under /api/*)

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Forbidden");
});

var app = builder.Build();

// Provider stores are provisioned externally; startup verifies and resolves the selected store.
await app.Services.GetRequiredService<IPermissionProviderInitializer>().InitializeAsync();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Expose the signed-in user's highest role to the layout badge (live per request).
app.Use(
    async (http, next) =>
    {
        if (http.User.Identity?.Name is { } username)
            http.Items["Role"] = await http
                .RequestServices.GetRequiredService<RoleResolver>()
                .GetHighestRoleAsync(username);
        var permissions = http.RequestServices.GetRequiredService<IPermissionService>();
        http.Items["AuthorizationProvider"] = permissions.ProviderDisplayName;
        await next();
    }
);

app.MapRazorPages();
app.MapControllers();
app.MapAuthEndpoints();
app.MapPostEndpoints();
app.MapAccessEndpoints();
app.MapMeEndpoints();
if (authorizationProvider.Provider == AuthorizationProviders.OpenFga)
    app.MapStoreEndpoints();
app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

app.Run();
