using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Security;

namespace PersonalTools.Pages;
[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public bool RememberMe { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public IActionResult OnGet()
    {
        // A remembered Case Tycoon guest is allowed to replace the active identity with a full
        // account. The separate protected guest ticket remains available for the next sign-out.
        if (User.Identity?.IsAuthenticated != true ||
            User.HasClaim(AppAuthorizationPolicies.AccountTypeClaim, AppAuthorizationPolicies.GuestAccount))
            return Page();
        return LocalRedirect("/");
    }
}
