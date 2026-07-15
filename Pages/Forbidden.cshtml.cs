using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages;

[AllowAnonymous]
public sealed class ForbiddenModel : PageModel
{
    public void OnGet() { }
}
