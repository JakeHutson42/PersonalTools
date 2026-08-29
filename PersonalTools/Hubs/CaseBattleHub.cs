using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PersonalTools.Classes.CaseBattles;
using System.Security.Claims;
namespace PersonalTools.Hubs;

[Authorize]
public sealed class CaseBattleHub(ICaseBattleFuncs battles) : Hub
{
    public const string EventChanged = "BattleChanged";
    public const string EventInvitation = "CaseBattleInvitation";
    public static string Group(Guid battleId) => $"case-battle:{battleId:D}";
    public static string UserGroup(Guid userId) => $"case-battle-user:{userId:D}";

    public async Task JoinNotifications()
    {
        string? rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out Guid userId)) throw new HubException("You are not signed in.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), Context.ConnectionAborted);
    }

    // Joining a SignalR group is an authorization boundary, not a client preference. A caller
    // can only subscribe after the same participant-scoped detail lookup used by the room page.
    public async Task JoinBattle(Guid battleId)
    {
        string? rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out Guid userId)) throw new HubException("You are not signed in.");
        await battles.GetDetail(userId, battleId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(battleId), Context.ConnectionAborted);
    }
}
