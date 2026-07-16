using DotNetEnv;
using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Endpoints;
using FgaPoc.Fga;
using FgaPoc.Options;

// Load .env into the process environment before configuration is built.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var config = builder.Configuration;

// All settings come from environment variables (see .env) — no appsettings.json.
builder.Services.AddSingleton(
    new FgaOptions
    {
        ApiUrl = config["FGA_API_URL"] ?? "http://localhost:8080",
        StoreName = config["FGA_STORE_NAME"] ?? "fga-blog-poc",
    }
);
builder.Services.AddSingleton(
    new BlogDbOptions
    {
        ConnectionString =
            config["BLOG_DB_CONNECTION"]
            ?? throw new InvalidOperationException("BLOG_DB_CONNECTION is required"),
    }
);

builder.Services.AddBlogData();
builder.Services.AddFga();
builder.Services.AddAppAuth();

builder.Services.AddControllers(); // MVC controllers under /mvc/* (alongside the minimal APIs under /api/*)

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Forbidden");
});

var app = builder.Build();

// Database schema/seed and the OpenFGA store are provisioned by docker-compose (Flyway +
// db/fga); the app only resolves which store to talk to.
await app.Services.GetRequiredService<FgaStoreResolver>().ResolveAsync();

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
        await next();
    }
);

app.MapRazorPages();
app.MapControllers();
app.MapAuthEndpoints();
app.MapPostEndpoints();
app.MapAccessEndpoints();
app.MapMeEndpoints();
app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();

app.Run();
