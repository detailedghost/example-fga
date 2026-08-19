using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace FgaPoc.Authorization;

// Each handler translates an ASP.NET authorization requirement into a provider check.
// The signed-in username (cookie Name claim) becomes the provider's user principal.

public sealed class CanCreatePostHandler(IPermissionService permissions)
    : AuthorizationHandler<CanCreatePostRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanCreatePostRequirement requirement
    )
    {
        var username = context.User.Identity?.Name;
        if (username is not null && await permissions.CanCreatePostAsync(username))
            context.Succeed(requirement);
    }
}

public sealed class CanManageAccessHandler(IPermissionService permissions)
    : AuthorizationHandler<CanManageAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanManageAccessRequirement requirement
    )
    {
        var username = context.User.Identity?.Name;
        if (username is not null && await permissions.CanManageAccessAsync(username))
            context.Succeed(requirement);
    }
}

public sealed class PostOperationHandler(IPermissionService permissions)
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
            "post:read" => await permissions.CanReadPostAsync(username, post),
            "post:edit" => await permissions.CanEditPostAsync(username, post),
            "post:delete" => await permissions.CanDeletePostAsync(username, post),
            _ => false,
        };
        if (allowed)
            context.Succeed(requirement);
    }
}
