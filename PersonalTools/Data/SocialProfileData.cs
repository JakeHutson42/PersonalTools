using MySqlConnector;
using PersonalTools.Entities;

namespace PersonalTools.Data;

public interface ISocialProfileData
{
    Task<SocialProfileObj?> GetProfile(Guid viewerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<List<SocialProfileObj>> GetFriends(Guid userId, CancellationToken cancellationToken = default);
    Task<List<SocialProfileObj>> Search(Guid userId, string query, int limit, CancellationToken cancellationToken = default);
    Task AddFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default);
    Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default);
    Task<(int Global, int Friends)> GetOnlineCounts(Guid userId, CancellationToken cancellationToken = default);
    Task TouchPresence(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class SocialProfileData(IMariaDbDataAccess database) : ISocialProfileData
{
    public Task<SocialProfileObj?> GetProfile(Guid viewerUserId, Guid targetUserId, CancellationToken cancellationToken = default) =>
        database.GetDataSP("sp_social_profile_get", Read, Params(("p_viewer_user_id", viewerUserId), ("p_target_user_id", targetUserId)), cancellationToken);
    public Task<List<SocialProfileObj>> GetFriends(Guid userId, CancellationToken cancellationToken = default) =>
        database.GetBulkDataSP("sp_social_friends_get", Read, Params(("p_user_id", userId)), cancellationToken);
    public Task<List<SocialProfileObj>> Search(Guid userId, string query, int limit, CancellationToken cancellationToken = default) =>
        database.GetBulkDataSP("sp_social_users_search", Read, Params(("p_user_id", userId), ("p_query", query), ("p_limit", limit)), cancellationToken);
    public Task AddFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_social_friend_add", Params(("p_user_id", userId), ("p_friend_user_id", friendUserId)), cancellationToken);
    public Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_social_friend_remove", Params(("p_user_id", userId), ("p_friend_user_id", friendUserId)), cancellationToken);
    public async Task<(int Global, int Friends)> GetOnlineCounts(Guid userId, CancellationToken cancellationToken = default)
    {
        var counts = await database.GetDataSP("sp_social_online_counts_get", reader => (Global: reader.GetInt32("GlobalCount"), Friends: reader.GetInt32("FriendsCount")), Params(("p_user_id", userId)), cancellationToken);
        return counts;
    }
    public Task TouchPresence(Guid userId, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_live_winners_presence_touch", Params(("p_user_id", userId)), cancellationToken);
    private static SocialProfileObj Read(MySqlDataReader reader) => new()
    {
        UserId = reader.GetGuid("UserId"), AccountId = reader.GetInt64("AccountId"), Username = reader.GetString("Username"),
        DisplayName = reader.GetString("DisplayName"), Avatar = reader.GetString("Avatar"), IsOnline = reader.GetBoolean("IsOnline"),
        LastSeenUtc = reader.IsDBNull(reader.GetOrdinal("LastSeenUtc")) ? null : DateTime.SpecifyKind(reader.GetDateTime("LastSeenUtc"), DateTimeKind.Utc), IsFriend = reader.GetBoolean("IsFriend")
    };
    private static MySqlParameter[] Params(params (string Name, object Value)[] values) => values.Select(value => new MySqlParameter(value.Name, value.Value)).ToArray();
}
