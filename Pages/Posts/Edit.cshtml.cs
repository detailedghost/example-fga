using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages.Posts;

public sealed class EditModel(PostRepository posts, IAuthorizationService authz) : PageModel
{
    public required Post Post { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var post = await posts.GetByIdAsync(id, ct);
        if (post is null)
            return NotFound();

        if (!(await authz.AuthorizeAsync(User, post, PostOperations.Edit)).Succeeded)
            return Forbid();

        Post = post;
        return Page();
    }
}
