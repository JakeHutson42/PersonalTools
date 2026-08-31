namespace PersonalTools.Entities.CaseBattles;

public static class CaseBattleModes
{
    public const string Duel = "duel";
    public const string FreeForAll3 = "ffa-3";
    public const string FreeForAll4 = "ffa-4";
    public const string Teams2v2 = "teams-2v2";

    public static int PlayerCount(string mode) => mode switch
    {
        Duel => 2,
        FreeForAll3 => 3,
        FreeForAll4 => 4,
        Teams2v2 => 4,
        _ => 0
    };

    // Only modes with a complete server execution and settlement path belong here. Runtime
    // feature flags still decide whether an implemented mode is available to players.
    public static bool IsEnabled(string mode) => mode is Duel or FreeForAll3 or FreeForAll4;
}

public sealed class CaseBattleFeatureOptions
{
    public bool Enabled { get; init; }
}

public sealed class CaseBattleCreateRequestObj { public string Mode { get; set; } = string.Empty; public Guid? InvitedUserId { get; set; } public List<Guid> InvitedUserIds { get; set; } = []; public bool UseBot { get; set; } public List<string> CaseKeys { get; set; } = []; }
public sealed class CaseBattleInviteRequestObj { public List<Guid> InvitedUserIds { get; set; } = []; }
public sealed class CaseBattleBotStatusObj { public bool CaseBattlesEnabled { get; set; } public bool FreeForAll3Enabled { get; set; } public bool FreeForAll4Enabled { get; set; } public bool Enabled { get; set; } public int BattlesAttempted { get; set; } public int BattlesWon { get; set; } public int SkinsDiscarded { get; set; } public decimal ValueDiscarded { get; set; } }
public sealed class CaseBattleTimingSettingsObj
{
    public int MaxCasesPerBattle { get; set; } = 20;
    public int ReadyPauseMs { get; set; } = 900;
    public int ReadyCountdownMs { get; set; } = 3000;
    public int PreSpinPauseMs { get; set; } = 2500;
    public int SpinDurationMs { get; set; } = 5000;
    public int LandedResultPauseMs { get; set; } = 750;
    public int RoundRevealPauseMs { get; set; } = 1650;
    public int ResultsPauseMs { get; set; } = 1100;
    public int WinnerIntroPauseMs { get; set; } = 2500;
    public int WinnerTallyDurationMs { get; set; } = 4200;
    public int WinnerVerdictPauseMs { get; set; } = 1800;
    public int WinnerTransferDurationMs { get; set; } = 3500;
}
public sealed class CaseBattleInvitableUserObj { public Guid UserId { get; set; } public string DisplayName { get; set; } = string.Empty; }
public sealed class CaseBattleInvitationObj { public Guid BattleId { get; set; } public string CreatorDisplayName { get; set; } = string.Empty; public List<string> CaseKeys { get; set; } = []; public DateTime ExpiresUtc { get; set; } }
public sealed class CaseBattlePendingCreatedObj { public Guid BattleId { get; set; } public string OpponentDisplayName { get; set; } = string.Empty; public int CaseCount { get; set; } public DateTime ExpiresUtc { get; set; } }
public sealed class CaseBattleBuyAllRequestObj { public List<string> CaseKeys { get; set; } = []; }
public sealed class CaseBattleBuyAllResultObj { public int PurchasedQuantity { get; set; } public long StarsSpent { get; set; } public long GbpPenceSpent { get; set; } public int StarsBalance { get; set; } public long GbpPenceBalance { get; set; } }

public class CaseBattleSummaryObj
{
    public Guid BattleId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CreatorUserId { get; set; }
    public int RequiredPlayers { get; set; }
    public int JoinedPlayers { get; set; }
    public List<string> CaseKeys { get; set; } = [];
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public Guid? WinningUserId { get; set; }
    public int? WinningTeam { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsParticipant { get; set; }
}

public sealed class CaseBattleParticipantObj
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ProfileAvatar { get; set; } = string.Empty;
    public int Seat { get; set; }
    public int Team { get; set; }
    public bool IsReady { get; set; }
    public decimal TotalValue { get; set; }
    public int OverflowReservedSlots { get; set; }
}

public sealed class CaseBattleDetailObj : CaseBattleSummaryObj
{
    public List<CaseBattleParticipantObj> Participants { get; set; } = [];
    public List<CaseBattleSeatInvitationObj> Invitations { get; set; } = [];
    public decimal LockedPotValue { get; set; }
    public bool IsCreator { get; set; }
    public List<CaseBattlePullObj> Pulls { get; set; } = [];
}

public sealed class CaseBattleSeatInvitationObj
{
    public Guid InvitedUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}

public sealed class CaseBattlePullObj
{
    public Guid BattleRollId { get; set; }
    public Guid OriginalOwnerUserId { get; set; }
    public string OriginalOwnerDisplayName { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Wear { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string RarityColor { get; set; } = string.Empty;
    public decimal LockedValue { get; set; }
    public string? Delivery { get; set; }
}

public sealed class CaseBattleHistoryObj
{
    public Guid BattleId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Won { get; set; }
    public decimal PersonalTotal { get; set; }
    public decimal AwardedValue { get; set; }
    public DateTime SettledUtc { get; set; }
}

public sealed class CaseBattleAdminReconciliationObj
{
    public Guid BattleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string CreatorDisplayName { get; set; } = string.Empty;
    public int JoinedPlayers { get; set; }
    public int CaseCount { get; set; }
    public int ReservedCaseCount { get; set; }
    public int StagedRollCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? StartedUtc { get; set; }
    public string Attention { get; set; } = string.Empty;
}

// Internal transport only. These records are populated from the participant-scoped execution
// plan and are never accepted from an HTTP request.
public sealed class CaseBattleRollPlanObj
{
    public Guid UserId { get; set; }
    public int Seat { get; set; }
    public int RoundNumber { get; set; }
    public string CaseKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public Guid? PriceSnapshotId { get; set; }
}

public sealed class CaseBattleServerRollObj
{
    public Guid OriginalOwnerUserId { get; set; }
    public int RoundNumber { get; set; }
    public Guid OpeningId { get; set; }
    public string CaseKey { get; set; } = string.Empty;
    public string SourceItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string MarketHashName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string RarityKey { get; set; } = string.Empty;
    public string RarityName { get; set; } = string.Empty;
    public string RarityColor { get; set; } = string.Empty;
    public string Wear { get; set; } = string.Empty;
    public bool IsStatTrak { get; set; }
    public bool IsRareSpecial { get; set; }
    public bool SupportsStatTrak { get; set; }
}
