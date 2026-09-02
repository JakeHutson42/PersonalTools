using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PersonalTools.Classes.CaseBattles;
using PersonalTools.Classes.CaseOpening;
using System.Security.Claims;
namespace PersonalTools.Hubs;

[Authorize(Policy = PersonalTools.Security.AppAuthorizationPolicies.CaseTycoonAccess)]
public sealed class CaseBattleHub(ICaseBattleFuncs battles, ICaseOpeningFuncs caseOpening) : Hub
{
    public const string EventChanged = "BattleChanged";
    public const string EventInvitation = "CaseBattleInvitation";
    public const string EventReaction = "CaseBattleReaction";
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
        Context.Items[$"case-battle-joined:{battleId:D}"] = true;
    }

    public async Task SendReaction(Guid battleId, string reactionKey)
    {
        string? rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out Guid userId)) throw new HubException("You are not signed in.");
        var reaction = await caseOpening.GetOwnedCaseBattleReaction(userId, reactionKey, Context.ConnectionAborted);
        DateTime now = DateTime.UtcNow;
        if (!Context.Items.ContainsKey($"case-battle-joined:{battleId:D}")) throw new HubException("Join the battle before reacting.");
        if (Context.Items.TryGetValue("case-battle-reaction-at", out object? previous) && previous is DateTime last && (now - last).TotalMilliseconds < 250)
            throw new HubException("Please wait a moment before reacting again.");
        Context.Items["case-battle-reaction-at"] = now;
        await Clients.Group(Group(battleId)).SendAsync(EventReaction, new { kind=reaction.Kind, value=reaction.Value }, Context.ConnectionAborted);
    }

    public async Task SendEffect(Guid battleId, string effectKey)
    {
        string? rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out _)) throw new HubException("You are not signed in.");
        if (!Context.Items.ContainsKey($"case-battle-joined:{battleId:D}")) throw new HubException("Join the battle before sending an effect.");
        string[] allowed = ["ak-rain", "gold-burst", "neon-storm"];
        if (!allowed.Contains(effectKey, StringComparer.Ordinal)) throw new HubException("That arena effect is unavailable.");
        DateTime now = DateTime.UtcNow;
        if (Context.Items.TryGetValue("case-battle-effect-at", out object? previous) && previous is DateTime last && (now-last).TotalSeconds < 3) throw new HubException("Please wait before sending another arena effect.");
        Context.Items["case-battle-effect-at"] = now;
        await Clients.Group(Group(battleId)).SendAsync("CaseBattleEffect", new { effectKey }, Context.ConnectionAborted);
    }
}
