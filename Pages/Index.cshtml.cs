using FgaPoc.Authorization;
using FgaPoc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages;

public sealed class IndexModel(PostRepository posts, IAuthorizationService authz) : PageModel
{
    public IReadOnlyList<Post> Posts { get; private set; } = [];
    public bool CanCreatePost { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Posts = await posts.GetAllAsync(ct);
        CanCreatePost = (await authz.AuthorizeAsync(User, Policies.CanCreatePost)).Succeeded;
    }
}
