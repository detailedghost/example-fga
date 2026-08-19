using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages.Admin;

public sealed record UserAccessRow(string Username, IReadOnlySet<string> DirectRoles);

public sealed class AccessModel(
    UserRepository users,
    IPermissionService permissions,
    IAuthorizationService authz
) : PageModel
{
    public IReadOnlyList<UserAccessRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Roles => BlogAuthorizationModel.Roles;
    public string Provider => permissions.ProviderDisplayName;
    public string? CurrentUser => User.Identity?.Name;

    /// <summary>Highest directly-granted role, in the nested admin→reader order.</summary>
    public string? EffectiveRole(UserAccessRow row) =>
        Roles.FirstOrDefault(row.DirectRoles.Contains);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!(await authz.AuthorizeAsync(User, Policies.CanManageAccess)).Succeeded)
            return Forbid();

        // One provider read powers the whole grid — no per-cell checks.
        var directRolesByUser = (await permissions.ReadRoleAssignmentsAsync(ct))
            .GroupBy(a => a.Username)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<string>)g.Select(a => a.Role).ToHashSet());

        var allUsers = await users.GetAllAsync(ct);
        Rows = allUsers
            .Select(u => new UserAccessRow(
                u.Username,
                directRolesByUser.GetValueOrDefault(u.Username, new HashSet<string>())
            ))
            .ToList();
        return Page();
    }
}
