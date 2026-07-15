using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages.Posts;

public sealed class DetailsModel(PostRepository posts, IAuthorizationService authz) : PageModel
{
    public required Post Post { get; set; }
    public bool CanEdit { get; private set; }
    public bool CanDelete { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        var post = await posts.GetByIdAsync(id, ct);
        if (post is null)
            return NotFound();

        if (!(await authz.AuthorizeAsync(User, post, PostOperations.Read)).Succeeded)
            return Forbid();

        Post = post;
        CanEdit = (await authz.AuthorizeAsync(User, post, PostOperations.Edit)).Succeeded;
        CanDelete = (await authz.AuthorizeAsync(User, post, PostOperations.Delete)).Succeeded;
        return Page();
    }
}
