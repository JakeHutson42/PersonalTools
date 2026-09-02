using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Security;
using PersonalTools.Data.CaseOpening;

namespace PersonalTools.Pages.CaseOpening;

[AllowAnonymous]
public sealed class IndexModel(ICaseOpeningData caseOpeningData) : PageModel
{
    public bool GuestAccessEnabled { get; private set; } = true;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        GuestAccessEnabled = await caseOpeningData.GetGuestAccessEnabled(cancellationToken);
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
    }
}
