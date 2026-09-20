using System.Text.RegularExpressions;

namespace PersonalTools.Tests;

public sealed class SocialDatabaseContractTests
{
    private static readonly string[] RequiredProcedures =
    [
        "sp_social_profile_get",
        "sp_social_friends_get",
        "sp_social_friend_requests_get",
        "sp_social_users_search",
        "sp_social_friend_add",
        "sp_social_friend_remove",
        "sp_social_friend_request_accept",
        "sp_social_friend_request_deny",
        "sp_social_online_counts_get",
        "sp_live_winners_presence_touch"
    ];

    [Fact]
    public void CanonicalSchema_DeclaresCompleteSocialContract()
    {
        string sql = File.ReadAllText(ProjectFile("PersonalTools", "PersonalTools.Database.sql"));

        AssertSocialContract(sql);
    }

    [Fact]
    public void RepairMigration_IsSelfContainedAndDeclaresCompleteSocialContract()
    {
        string sql = File.ReadAllText(ProjectFile("PersonalTools", "DatabaseUpdates", "2026-09-03-case-tycoon-social-schema-repair.sql"));

        Assert.Contains("ALTER TABLE Users ADD COLUMN IF NOT EXISTS AccountId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER TABLE Users ADD COLUMN IF NOT EXISTS Username", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS UserLivePresence", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS UserFriends", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS UserFriendRequests", sql, StringComparison.OrdinalIgnoreCase);
        AssertSocialContract(sql);
    }

    [Fact]
    public void SearchCollationFixMigration_UsesCollationSafeComparisons()
    {
        string sql = File.ReadAllText(ProjectFile("PersonalTools", "DatabaseUpdates", "2026-09-04-case-tycoon-social-search-collation-fix.sql"));

        AssertSearchIsCollationSafe(sql);
    }

    private static void AssertSocialContract(string sql)
    {
        foreach (string procedure in RequiredProcedures)
        {
            Assert.Matches(
                new Regex($@"CREATE\s+PROCEDURE\s+{Regex.Escape(procedure)}\b", RegexOptions.IgnoreCase),
                sql);
        }

        AssertSearchIsCollationSafe(sql);
    }

    private static void AssertSearchIsCollationSafe(string sql)
    {
        string searchProcedure = Regex.Match(sql, @"CREATE\s+PROCEDURE\s+sp_social_users_search\b(?<body>.*?)END//", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["body"].Value;
        Assert.NotEmpty(searchProcedure);
        Assert.Contains("BINARY setting.UserId=BINARY u.UserId", searchProcedure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BINARY presence.UserId=BINARY u.UserId", searchProcedure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COLLATE utf8mb4_unicode_ci LIKE", searchProcedure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BINARY CAST(u.AccountId AS CHAR)=BINARY", searchProcedure, StringComparison.OrdinalIgnoreCase);
    }

    private static string ProjectFile(params string[] path)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PersonalTools.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory!.FullName, .. path]);
    }
}
