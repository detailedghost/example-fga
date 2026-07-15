using FgaPoc.Authorization;
using FgaPoc.Data;
using FgaPoc.Fga;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages.Admin;

public sealed record UserAccessRow(string Username, IReadOnlySet<string> DirectRoles);

public sealed class AccessModel(UserRepository users, FgaService fga, IAuthorizationService authz)
    : PageModel
{
    public IReadOnlyList<UserAccessRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Roles => FgaService.Roles;
    public string? CurrentUser => User.Identity?.Name;

    /// <summary>Highest directly-granted role, in the nested admin→reader order.</summary>
    public string? EffectiveRole(UserAccessRow row) =>
        Roles.FirstOrDefault(row.DirectRoles.Contains);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!(await authz.AuthorizeAsync(User, Policies.CanManageAccess)).Succeeded)
            return Forbid();

        // One read of the blog's role tuples powers the whole grid — no per-cell checks.
        var directRolesByUser = (await fga.ReadRoleAssignmentsAsync(ct))
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
