using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Security;

namespace PersonalTools.Pages.Chess;

[Authorize(Policy = AppAuthorizationPolicies.RegisteredUser)]
public sealed class PracticeModel : PageModel;
