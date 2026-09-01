namespace PersonalTools.Entities;

public sealed class SocialProfileObj
{
    public Guid UserId { get; set; }
    public long AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = "😎";
    public bool IsOnline { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public bool IsFriend { get; set; }
}

public sealed record SocialPresenceObj(Guid UserId, bool IsOnline, DateTime LastSeenUtc);

public sealed class SocialSummaryObj
{
    public SocialProfileObj Profile { get; set; } = new();
    public List<SocialProfileObj> Friends { get; set; } = [];
    public int GlobalOnlineCount { get; set; }
    public int FriendsOnlineCount { get; set; }
}
