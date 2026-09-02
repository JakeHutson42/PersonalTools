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
        if (User.Identity?.IsAuthenticated != true) return Page();
        return User.HasClaim(AppAuthorizationPolicies.AccountTypeClaim, AppAuthorizationPolicies.GuestAccount)
            ? RedirectToPage("/CaseOpening/Index")
            : LocalRedirect("/");
    }
}
