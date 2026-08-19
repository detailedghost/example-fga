using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FgaPoc.Endpoints;

/// <summary>
/// Blog post mutations. Every handler is thin: authorize, touch the repository + provider, redirect.
/// Create is a coarse policy (blog-level "writer"); edit/delete are per-post resource checks.
/// </summary>
public static class PostEndpoints
{
    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/posts").RequireAuthorization().DisableAntiforgery();

        group
            .MapPost(
                "/",
                async (
                    [FromForm] string title,
                    [FromForm] string body,
                    HttpContext http,
                    PostRepository posts,
                    IPermissionService permissions
                ) =>
                {
                    var author = http.User.Identity!.Name!;
                    var id = await posts.CreateAsync(title, body, author);
                    await permissions.LinkNewPostAsync(id, author);
                    return Results.Redirect($"/Posts/Details?id={id}");
                }
            )
            .RequireAuthorization(Policies.CanCreatePost);

        group.MapPost(
            "/{id:int}/edit",
            async (
                int id,
                [FromForm] string title,
                [FromForm] string body,
                HttpContext http,
                PostRepository posts,
                IAuthorizationService authz
            ) =>
            {
                var post = await posts.GetByIdAsync(id);
                if (post is null)
                    return Results.NotFound();
                if (!(await authz.AuthorizeAsync(http.User, post, PostOperations.Edit)).Succeeded)
                    return Results.Forbid();

                await posts.UpdateAsync(id, title, body);
                return Results.Redirect($"/Posts/Details?id={id}");
            }
        );

        group.MapPost(
            "/{id:int}/delete",
            async (
                int id,
                HttpContext http,
                PostRepository posts,
                IPermissionService permissions,
                IAuthorizationService authz
            ) =>
            {
                var post = await posts.GetByIdAsync(id);
                if (post is null)
                    return Results.NotFound();
                if (!(await authz.AuthorizeAsync(http.User, post, PostOperations.Delete)).Succeeded)
                    return Results.Forbid();

                await posts.DeleteAsync(id);
                await permissions.UnlinkPostAsync(id, post.AuthorUsername);
                return Results.Redirect("/");
            }
        );
    }
}
