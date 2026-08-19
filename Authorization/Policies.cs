using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace FgaPoc.Authorization;

/// <summary>Named policy keys for the coarse (non-resource) authorization checks.</summary>
public static class Policies
{
    public const string CanCreatePost = "CanCreatePost";
    public const string CanManageAccess = "CanManageAccess";
}

/// <summary>Per-post operations, authorized against a <see cref="Data.Post"/> resource.</summary>
public static class PostOperations
{
    public static readonly OperationAuthorizationRequirement Read = new() { Name = "post:read" };
    public static readonly OperationAuthorizationRequirement Edit = new() { Name = "post:edit" };
    public static readonly OperationAuthorizationRequirement Delete = new()
    {
        Name = "post:delete",
    };
}

public sealed class CanCreatePostRequirement : IAuthorizationRequirement;

public sealed class CanManageAccessRequirement : IAuthorizationRequirement;
