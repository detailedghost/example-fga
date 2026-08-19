using FgaPoc.Data;

namespace FgaPoc.Authorization;

public sealed record RoleAssignment(string Username, string Role);

public static class PermissionNames
{
    public const string ReadPosts = "read:posts";
    public const string CreatePosts = "create:posts";
    public const string EditPosts = "edit:posts";
    public const string DeletePosts = "delete:posts";
    public const string ManageAccess = "manage:access";
}

public static class BlogAuthorizationModel
{
    // Most to least privileged. Each role inherits every role to its right.
    public static readonly IReadOnlyList<string> Roles = ["admin", "editor", "writer", "reader"];

    public static void EnsureKnownRole(string role)
    {
        if (!Roles.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'", nameof(role));
    }

    public static IReadOnlyList<string> PermissionsForRole(string? role) =>
        role switch
        {
            "admin" =>
            [
                PermissionNames.ReadPosts,
                PermissionNames.CreatePosts,
                PermissionNames.EditPosts,
                PermissionNames.DeletePosts,
                PermissionNames.ManageAccess,
            ],
            "editor" =>
            [
                PermissionNames.ReadPosts,
                PermissionNames.CreatePosts,
                PermissionNames.EditPosts,
                PermissionNames.DeletePosts,
            ],
            "writer" => [PermissionNames.ReadPosts, PermissionNames.CreatePosts],
            "reader" => [PermissionNames.ReadPosts],
            _ => [],
        };
}

/// <summary>The application-facing authorization contract implemented by either provider.</summary>
public interface IPermissionService
{
    string ProviderId { get; }
    string ProviderDisplayName { get; }

    Task<bool> CanCreatePostAsync(string username, CancellationToken ct = default);
    Task<bool> CanManageAccessAsync(string username, CancellationToken ct = default);
    Task<bool> CanReadPostAsync(string username, Post post, CancellationToken ct = default);
    Task<bool> CanEditPostAsync(string username, Post post, CancellationToken ct = default);
    Task<bool> CanDeletePostAsync(string username, Post post, CancellationToken ct = default);

    Task<string?> GetHighestRoleAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(
        string username,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<RoleAssignment>> ReadRoleAssignmentsAsync(CancellationToken ct = default);
    Task GrantRoleAsync(string username, string role, CancellationToken ct = default);
    Task RevokeRoleAsync(string username, string role, CancellationToken ct = default);

    Task LinkNewPostAsync(int postId, string ownerUsername, CancellationToken ct = default);
    Task UnlinkPostAsync(int postId, string ownerUsername, CancellationToken ct = default);
}

public interface IPermissionProviderInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}
