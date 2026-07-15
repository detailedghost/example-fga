using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FgaPoc.Pages;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    public bool ShowError { get; private set; }

    public void OnGet([FromQuery] string? error) => ShowError = error == "1";
}
