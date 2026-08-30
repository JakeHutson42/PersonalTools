using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.CaseBattles;
using System.Security.Claims;

namespace PersonalTools.Pages.CaseOpening.Battles;

public sealed class LobbyModel(ICaseBattleFuncs battles) : PageModel
{
    public Guid BattleId { get; private set; }
    public async Task<IActionResult> OnGet(Guid battleId, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try { await battles.GetDetail(userId, battleId, cancellationToken); BattleId = battleId; return Page(); }
        catch (KeyNotFoundException) { return RedirectToPage("/CaseOpening/Index"); }
    }
}
