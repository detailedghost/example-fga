using FgaPoc.Data;
using FgaPoc.Fga;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace FgaPoc.Authorization;

// Each handler translates an ASP.NET authorization requirement into an OpenFGA check.
// The signed-in username (cookie Name claim) becomes the FGA "user:{name}" subject.

public sealed class CanCreatePostHandler(FgaService fga)
    : AuthorizationHandler<CanCreatePostRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanCreatePostRequirement requirement
    )
    {
        var username = context.User.Identity?.Name;
        if (username is not null && await fga.CanCreatePostAsync(username))
            context.Succeed(requirement);
    }
}

public sealed class CanManageAccessHandler(FgaService fga)
    : AuthorizationHandler<CanManageAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanManageAccessRequirement requirement
    )
    {
        var username = context.User.Identity?.Name;
        if (username is not null && await fga.CanManageAccessAsync(username))
            context.Succeed(requirement);
    }
}

public sealed class PostOperationHandler(FgaService fga)
    : AuthorizationHandler<OperationAuthorizationRequirement, Post>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Post post
    )
    {
        var username = context.User.Identity?.Name;
        if (username is null)
            return;

        var allowed = requirement.Name switch
        {
            "post:read" => await fga.CanReadPostAsync(username, post.Id),
            "post:edit" => await fga.CanEditPostAsync(username, post.Id),
            "post:delete" => await fga.CanDeletePostAsync(username, post.Id),
            _ => false,
        };
        if (allowed)
            context.Succeed(requirement);
    }
}
