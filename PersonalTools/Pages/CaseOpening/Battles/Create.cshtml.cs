using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PersonalTools.Entities.CaseBattles;
namespace PersonalTools.Pages.CaseOpening.Battles;
public sealed class CreateModel(IOptions<CaseBattleFeatureOptions> options) : PageModel
{
    public IActionResult OnGet() => options.Value.Enabled ? Page() : RedirectToPage("/CaseOpening/Index");
}
