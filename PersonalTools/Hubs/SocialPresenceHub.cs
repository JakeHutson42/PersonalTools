using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PersonalTools.Data;
using PersonalTools.Entities;

namespace PersonalTools.Hubs;

[Authorize]
public sealed class SocialPresenceHub(ISocialProfileData data) : Hub
{
    private static readonly ConcurrentDictionary<Guid, int> Connections = new();
    private Guid UserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        Guid userId = UserId;
        int count = Connections.AddOrUpdate(userId, 1, (_, current) => current + 1);
        await data.TouchPresence(userId, Context.ConnectionAborted);
        if (count == 1) await Clients.Others.SendAsync("PresenceChanged", new SocialPresenceObj(userId, true, DateTime.UtcNow), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public async Task Heartbeat()
    {
        await data.TouchPresence(UserId, Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Guid userId = UserId;
        int remaining = Connections.AddOrUpdate(userId, 0, (_, current) => Math.Max(0, current - 1));
        if (remaining == 0)
        {
            Connections.TryRemove(userId, out _);
            await Clients.Others.SendAsync("PresenceChanged", new SocialPresenceObj(userId, false, DateTime.UtcNow));
        }
        await base.OnDisconnectedAsync(exception);
    }
}
