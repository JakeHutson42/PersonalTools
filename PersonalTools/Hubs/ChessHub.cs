using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PersonalTools.Classes.ChessGames;
using PersonalTools.Security;

namespace PersonalTools.Hubs;

[Authorize(Policy = AppAuthorizationPolicies.RegisteredUser)]
public sealed class ChessHub(ChessGameService games) : Hub
{
    public static string Group(Guid id) => $"chess:{id:D}";
    public async Task JoinGame(Guid id)
    {
        if (!Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) throw new HubException("Sign in first.");
        try { await games.Get(id, userId, Context.ConnectionAborted); }
        catch (KeyNotFoundException) { throw new HubException("Game not found."); }
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(id), Context.ConnectionAborted);
    }
}
