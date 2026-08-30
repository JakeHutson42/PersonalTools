using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.CaseBattles;
using PersonalTools.Entities.CaseBattles;
using System.Security.Claims;

namespace PersonalTools.Pages.CaseOpening.Battles;

public sealed class IndexModel(ICaseBattleFuncs battles) : PageModel
{
    public Guid? BattleId { get; private set; }

    public async Task<IActionResult> OnGet(Guid? battleId, CancellationToken cancellationToken)
    {
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

        CaseBattleSummaryObj? active = await battles.GetActive(userId, cancellationToken);
        if (active is null) return RedirectToPage("/CaseOpening/Index");
        return active.Status == "waiting"
            ? RedirectToPage("/CaseOpening/Battles/Lobby", new { battleId = active.BattleId })
            : RedirectToPage("/CaseOpening/Battles/Index", new { battleId = active.BattleId });
    }
}
