using PersonalTools.Data;
using PersonalTools.Entities;

namespace PersonalTools.Classes;

public interface ISocialProfileFuncs
{
    Task<SocialSummaryObj> GetSummary(Guid userId, CancellationToken cancellationToken = default);
    Task<List<SocialProfileObj>> Search(Guid userId, string query, CancellationToken cancellationToken = default);
    Task AddFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default);
    Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default);
}

public sealed class SocialProfileFuncs(ISocialProfileData data) : ISocialProfileFuncs
{
    public async Task<SocialSummaryObj> GetSummary(Guid userId, CancellationToken cancellationToken = default)
    {
        SocialProfileObj profile = await data.GetProfile(userId, userId, cancellationToken) ?? throw new InvalidOperationException("Your profile could not be found.");
        List<SocialProfileObj> friends = await data.GetFriends(userId, cancellationToken);
        (int global, int friendCount) = await data.GetOnlineCounts(userId, cancellationToken);
        return new() { Profile = profile, Friends = friends, GlobalOnlineCount = global, FriendsOnlineCount = friendCount };
    }
    public Task<List<SocialProfileObj>> Search(Guid userId, string query, CancellationToken cancellationToken = default)
    {
        string value = query.Trim();
        if (value.Length is < 2 or > 100) throw new InvalidOperationException("Enter at least 2 characters of a username or name, or an account ID.");
        return data.Search(userId, value, 20, cancellationToken);
    }
    public Task AddFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default)
    {
        if (friendUserId == Guid.Empty || friendUserId == userId) throw new InvalidOperationException("Choose another player.");
        return data.AddFriend(userId, friendUserId, cancellationToken);
    }
    public Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default) => data.RemoveFriend(userId, friendUserId, cancellationToken);
}
