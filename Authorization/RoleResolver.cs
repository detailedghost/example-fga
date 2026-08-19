namespace FgaPoc.Authorization;

/// <summary>
/// Resolves a user's highest (most-privileged) blog role for display in the UI badge.
/// Roles are nested, so the first matching check from admin→reader is the effective role.
/// </summary>
public sealed class RoleResolver(IPermissionService permissions)
{
    public Task<string?> GetHighestRoleAsync(string username, CancellationToken ct = default) =>
        permissions.GetHighestRoleAsync(username, ct);
}
