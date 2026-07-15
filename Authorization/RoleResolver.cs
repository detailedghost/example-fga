using FgaPoc.Fga;

namespace FgaPoc.Authorization;

/// <summary>
/// Resolves a user's highest (most-privileged) blog role for display in the UI badge.
/// Roles are nested, so the first matching check from admin→reader is the effective role.
/// </summary>
public sealed class RoleResolver(FgaService fga)
{
    public async Task<string?> GetHighestRoleAsync(string username, CancellationToken ct = default)
    {
        foreach (var role in FgaService.Roles)
            if (await fga.HasBlogRoleAsync(username, role, ct))
                return role;
        return null;
    }
}
