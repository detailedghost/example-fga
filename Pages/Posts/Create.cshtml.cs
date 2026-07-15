using FgaPoc.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages.Posts;

public sealed class CreateModel(IAuthorizationService authz) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        if (!(await authz.AuthorizeAsync(User, Policies.CanCreatePost)).Succeeded)
            return Forbid();
        return Page();
    }
}
