using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.CaseBattles;
using PersonalTools.Entities.CaseBattles;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace PersonalTools.Pages.CaseOpening.Battles;

[Microsoft.AspNetCore.Authorization.Authorize(Policy=PersonalTools.Security.AppAuthorizationPolicies.CaseTycoonAccess)]
public sealed class IndexModel(ICaseBattleFuncs battles, IOptions<CaseBattleFeatureOptions> options) : PageModel
{
    public Guid? BattleId { get; private set; }

    public async Task<IActionResult> OnGet(Guid? battleId, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return RedirectToPage("/CaseOpening/Index");
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // A battle room is not a public destination.  It is only a resumable surface for a player
        // who is already in an unfinished battle; creation remains on the case-opening screen.
        if (battleId is not null)
        {
            try
            {
                CaseBattleDetailObj battle = await battles.GetDetail(userId, battleId.Value, cancellationToken);
                if (battle.Status == "waiting") return RedirectToPage("/CaseOpening/Battles/Lobby", new { battleId });
                BattleId = battleId;
            }
            catch (KeyNotFoundException) { return RedirectToPage("/CaseOpening/Index"); }
            return Page();
        }

        // With no battle id this route is the battle home. Active rooms are surfaced there as a
        // resumable card instead of unexpectedly redirecting the player away from their history.
        return Page();
    }
}
