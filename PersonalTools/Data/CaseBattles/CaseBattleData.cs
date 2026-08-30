using System.Text.Json;
using MySqlConnector;
using PersonalTools.Entities.CaseBattles;

namespace PersonalTools.Data.CaseBattles;

public interface ICaseBattleData
{
    Task Create(Guid battleId, Guid userId, string mode, List<string> caseKeys, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj?> Get(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj?> GetActive(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleDetailObj?> GetDetail(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task Join(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task SetReady(Guid battleId, Guid userId, bool isReady, CancellationToken cancellationToken = default);
    Task Start(Guid battleId, CancellationToken cancellationToken = default);
    Task StageRolls(Guid battleId, List<CaseBattleServerRollObj> rolls, CancellationToken cancellationToken = default);
    Task Settle(Guid battleId, CancellationToken cancellationToken = default);
    Task Cancel(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task Leave(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleRollPlanObj>> GetExecutionPlan(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetLockedMarketHashes(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleBuyAllResultObj?> BuyAll(Guid userId, List<(string CaseKey, int Quantity, int CostStars, long CostGbpPence)> purchases, CancellationToken cancellationToken = default);
    Task<List<CaseBattleHistoryObj>> GetHistory(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleAdminReconciliationObj>> GetAdminReconciliation(CancellationToken cancellationToken = default);
    Task Expire(Guid battleId, CancellationToken cancellationToken = default);
    Task CancelPendingAsAdmin(Guid battleId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default);
    Task SetInvite(Guid battleId, Guid creatorUserId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleInvitationObj>> GetPendingInvitations(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseBattlePendingCreatedObj>> GetPendingCreated(Guid userId, CancellationToken cancellationToken = default);
    Task AcceptInvite(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task DeclineInvite(Guid battleId, Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default);
    Task<CaseBattleTimingSettingsObj> GetTimingSettings(CancellationToken cancellationToken = default);
    Task SetTimingSettings(CaseBattleTimingSettingsObj settings, CancellationToken cancellationToken = default);
    Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task JoinBot(Guid battleId, CancellationToken cancellationToken = default);
}

public sealed class CaseBattleData(IMariaDbDataAccess database) : ICaseBattleData
{
    public Task Create(Guid battleId, Guid userId, string mode, List<string> caseKeys, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_create", Parameters(("p_battle_id", battleId), ("p_user_id", userId), ("p_mode", mode), ("p_case_keys", JsonSerializer.Serialize(caseKeys))), cancellationToken);
    public Task<CaseBattleSummaryObj?> Get(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        database.GetDataSP("sp_case_battles_get", ReadSummary, Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public Task<CaseBattleSummaryObj?> GetActive(Guid userId, CancellationToken cancellationToken = default) =>
        database.GetDataSP("sp_case_battles_active_get", ReadSummary, Parameters(("p_user_id", userId)), cancellationToken);
    public async Task<CaseBattleDetailObj?> GetDetail(Guid battleId, Guid userId, CancellationToken cancellationToken = default)
    {
        CaseBattleSummaryObj? summary = await Get(battleId, userId, cancellationToken);
        if (summary is null || !summary.IsParticipant) return null;
        CaseBattleDetailObj detail = new()
        {
            BattleId = summary.BattleId, CreatorUserId = summary.CreatorUserId, Mode = summary.Mode, Status = summary.Status,
            RequiredPlayers = summary.RequiredPlayers, JoinedPlayers = summary.JoinedPlayers, CaseKeys = summary.CaseKeys,
            CreatedUtc = summary.CreatedUtc, ExpiresUtc = summary.ExpiresUtc, WinningUserId = summary.WinningUserId, WinningTeam = summary.WinningTeam, IsParticipant = true
        };
        detail.Participants = await database.GetBulkDataSP("sp_case_battles_participants_get", reader => new CaseBattleParticipantObj
        {
            UserId = reader.GetGuid("UserId"), DisplayName = reader.GetString("DisplayName"), Seat = reader.GetInt32("Seat"),
            Team = reader.GetInt32("Team"), IsReady = reader.GetBoolean("IsReady"), TotalValue = reader.GetDecimal("TotalValue"),
            OverflowReservedSlots = reader.GetInt32("OverflowReservedSlots")
        }, Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
        detail.Pulls = await database.GetBulkDataSP("sp_case_battles_pulls_get", reader => new CaseBattlePullObj
        {
            BattleRollId = reader.GetGuid("BattleRollId"), OriginalOwnerUserId = reader.GetGuid("OriginalOwnerUserId"),
            OriginalOwnerDisplayName = reader.GetString("OriginalOwnerDisplayName"), RoundNumber = reader.GetInt32("RoundNumber"),
            ItemName = reader.GetString("ItemName"), Wear = reader.GetString("Wear"), ImageUrl = reader.GetString("ImageUrl"), RarityColor = reader.GetString("RarityColor"),
            LockedValue = reader.GetDecimal("LockedValue"), Delivery = reader.IsDBNull(reader.GetOrdinal("Delivery")) ? null : reader.GetString("Delivery")
        }, Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
        detail.LockedPotValue = detail.Pulls.Sum(item => item.LockedValue);
        return detail;
    }
    public Task Join(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_join", Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public Task SetReady(Guid battleId, Guid userId, bool isReady, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_ready_set", Parameters(("p_battle_id", battleId), ("p_user_id", userId), ("p_is_ready", isReady)), cancellationToken);
    public Task Start(Guid battleId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_start", Parameters(("p_battle_id", battleId)), cancellationToken);
    public Task StageRolls(Guid battleId, List<CaseBattleServerRollObj> rolls, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_rolls_stage", Parameters(("p_battle_id", battleId), ("p_rolls", JsonSerializer.Serialize(rolls, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))), cancellationToken);
    public Task Settle(Guid battleId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_settle_staged", Parameters(("p_battle_id", battleId)), cancellationToken);
    public Task Cancel(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_cancel", Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public Task Leave(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_leave", Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public Task<List<CaseBattleRollPlanObj>> GetExecutionPlan(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        database.GetBulkDataSP("sp_case_battles_execution_plan_get", reader => new CaseBattleRollPlanObj
        {
            UserId = reader.GetGuid("UserId"), Seat = reader.GetInt32("Seat"), RoundNumber = reader.GetInt32("RoundNumber"),
            CaseKey = reader.GetString("CaseKey"), Status = reader.GetString("Status"), Mode = reader.GetString("Mode"),
            PriceSnapshotId = reader.IsDBNull(reader.GetOrdinal("PriceSnapshotId")) ? null : reader.GetGuid("PriceSnapshotId")
        }, Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public async Task<HashSet<string>> GetLockedMarketHashes(Guid battleId, Guid userId, CancellationToken cancellationToken = default) =>
        (await database.GetBulkDataSP("sp_case_battles_locked_market_hashes_get", reader => reader.GetString("MarketHashName"), Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    public Task<CaseBattleBuyAllResultObj?> BuyAll(Guid userId, List<(string CaseKey, int Quantity, int CostStars, long CostGbpPence)> purchases, CancellationToken cancellationToken = default) =>
        database.GetDataSP("sp_case_battles_cases_buy_all", reader => new CaseBattleBuyAllResultObj { PurchasedQuantity=reader.GetInt32("PurchasedQuantity"), StarsSpent=reader.GetInt64("StarsSpent"), GbpPenceSpent=reader.GetInt64("GbpPenceSpent"), StarsBalance=reader.GetInt32("StarsBalance"), GbpPenceBalance=reader.GetInt64("GbpPenceBalance") }, Parameters(("p_user_id", userId), ("p_purchases", JsonSerializer.Serialize(purchases.Select(item => new { caseKey=item.CaseKey, quantity=item.Quantity, costStars=item.CostStars, costGbpPence=item.CostGbpPence })))), cancellationToken);
    public Task<List<CaseBattleHistoryObj>> GetHistory(Guid userId, CancellationToken cancellationToken = default) =>
        database.GetBulkDataSP("sp_case_battles_history_get", reader => new CaseBattleHistoryObj { BattleId=reader.GetGuid("BattleId"), Mode=reader.GetString("Mode"), Status=reader.GetString("Status"), Won=reader.GetBoolean("Won"), PersonalTotal=reader.GetDecimal("TotalValue"), AwardedValue=reader.GetDecimal("AwardedValue"), SettledUtc=reader.IsDBNull(reader.GetOrdinal("SettledUtc")) ? DateTime.MinValue : DateTime.SpecifyKind(reader.GetDateTime("SettledUtc"), DateTimeKind.Utc) }, Parameters(("p_user_id", userId)), cancellationToken);
    public Task<List<CaseBattleAdminReconciliationObj>> GetAdminReconciliation(CancellationToken cancellationToken = default) =>
        database.GetBulkDataSP("sp_case_battles_admin_reconciliation_get", reader => new CaseBattleAdminReconciliationObj
        {
            BattleId = reader.GetGuid("BattleId"), Status = reader.GetString("Status"), Mode = reader.GetString("Mode"), CreatorDisplayName = reader.GetString("CreatorDisplayName"),
            JoinedPlayers = reader.GetInt32("JoinedPlayers"), CaseCount = reader.GetInt32("CaseCount"), ReservedCaseCount = reader.GetInt32("ReservedCaseCount"), StagedRollCount = reader.GetInt32("StagedRollCount"),
            CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc), ExpiresUtc = DateTime.SpecifyKind(reader.GetDateTime("ExpiresUtc"), DateTimeKind.Utc),
            StartedUtc = reader.IsDBNull(reader.GetOrdinal("StartedUtc")) ? null : DateTime.SpecifyKind(reader.GetDateTime("StartedUtc"), DateTimeKind.Utc), Attention = reader.GetString("Attention")
        }, [], cancellationToken);
    public Task Expire(Guid battleId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_expire", Parameters(("p_battle_id", battleId)), cancellationToken);
    public Task CancelPendingAsAdmin(Guid battleId, CancellationToken cancellationToken = default) =>
        database.ExecuteSP("sp_case_battles_admin_cancel_pending", Parameters(("p_battle_id", battleId)), cancellationToken);
    public Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default) => database.GetBulkDataSP("sp_case_battles_invitable_users_get", reader => new CaseBattleInvitableUserObj { UserId=reader.GetGuid("UserId"), DisplayName=reader.GetString("DisplayName") }, Parameters(("p_user_id", userId)), cancellationToken);
    public Task SetInvite(Guid battleId, Guid creatorUserId, Guid invitedUserId, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_invite_set", Parameters(("p_battle_id", battleId), ("p_creator_user_id", creatorUserId), ("p_invited_user_id", invitedUserId)), cancellationToken);
    public Task<List<CaseBattleInvitationObj>> GetPendingInvitations(Guid userId, CancellationToken cancellationToken = default) => database.GetBulkDataSP("sp_case_battles_invitations_get", reader => new CaseBattleInvitationObj { BattleId=reader.GetGuid("BattleId"), CreatorDisplayName=reader.GetString("CreatorDisplayName"), CaseKeys=JsonSerializer.Deserialize<List<string>>(reader.GetString("CaseKeys")) ?? [], ExpiresUtc=DateTime.SpecifyKind(reader.GetDateTime("ExpiresUtc"), DateTimeKind.Utc) }, Parameters(("p_user_id", userId)), cancellationToken);
    public Task<List<CaseBattlePendingCreatedObj>> GetPendingCreated(Guid userId, CancellationToken cancellationToken = default) => database.GetBulkDataSP("sp_case_battles_pending_created_get", reader => new CaseBattlePendingCreatedObj { BattleId=reader.GetGuid("BattleId"), OpponentDisplayName=reader.GetString("OpponentDisplayName"), CaseCount=reader.GetInt32("CaseCount"), ExpiresUtc=DateTime.SpecifyKind(reader.GetDateTime("ExpiresUtc"), DateTimeKind.Utc) }, Parameters(("p_user_id", userId)), cancellationToken);
    public Task AcceptInvite(Guid battleId, Guid userId, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_invitation_accept", Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public Task DeclineInvite(Guid battleId, Guid userId, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_invitation_decline", Parameters(("p_battle_id", battleId), ("p_user_id", userId)), cancellationToken);
    public async Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default) => await database.GetDataSP("sp_case_battles_bot_status_get", reader => new CaseBattleBotStatusObj { CaseBattlesEnabled=reader.GetBoolean("CaseBattlesEnabled"), Enabled=reader.GetBoolean("Enabled"), BattlesAttempted=reader.GetInt32("BattlesAttempted"), BattlesWon=reader.GetInt32("BattlesWon"), SkinsDiscarded=reader.GetInt32("SkinsDiscarded"), ValueDiscarded=reader.GetDecimal("ValueDiscarded") }, [], cancellationToken) ?? new CaseBattleBotStatusObj();
    public async Task<CaseBattleTimingSettingsObj> GetTimingSettings(CancellationToken cancellationToken = default) => await database.GetDataSP("sp_case_battles_timing_settings_get", ReadTimingSettings, [], cancellationToken) ?? new CaseBattleTimingSettingsObj();
    public Task SetTimingSettings(CaseBattleTimingSettingsObj settings, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_timing_settings_set", Parameters(("p_max_cases_per_battle", settings.MaxCasesPerBattle), ("p_ready_pause_ms", settings.ReadyPauseMs), ("p_ready_countdown_ms", settings.ReadyCountdownMs), ("p_pre_spin_pause_ms", settings.PreSpinPauseMs), ("p_spin_duration_ms", settings.SpinDurationMs), ("p_landed_result_pause_ms", settings.LandedResultPauseMs), ("p_round_reveal_pause_ms", settings.RoundRevealPauseMs), ("p_results_pause_ms", settings.ResultsPauseMs), ("p_winner_intro_pause_ms", settings.WinnerIntroPauseMs), ("p_winner_tally_duration_ms", settings.WinnerTallyDurationMs), ("p_winner_verdict_pause_ms", settings.WinnerVerdictPauseMs), ("p_winner_transfer_duration_ms", settings.WinnerTransferDurationMs)), cancellationToken);
    public Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_feature_enabled_set", Parameters(("p_enabled", enabled)), cancellationToken);
    public Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_bot_enabled_set", Parameters(("p_enabled", enabled)), cancellationToken);
    public Task JoinBot(Guid battleId, CancellationToken cancellationToken = default) => database.ExecuteSP("sp_case_battles_bot_join", Parameters(("p_battle_id", battleId)), cancellationToken);

    private static CaseBattleSummaryObj ReadSummary(MySqlDataReader reader) => new()
    {
        BattleId = reader.GetGuid("BattleId"), CreatorUserId = reader.GetGuid("CreatorUserId"), Mode = reader.GetString("Mode"), Status = reader.GetString("Status"),
        RequiredPlayers = CaseBattleModes.PlayerCount(reader.GetString("Mode")), JoinedPlayers = reader.GetInt32("JoinedPlayers"),
        CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc), ExpiresUtc = DateTime.SpecifyKind(reader.GetDateTime("ExpiresUtc"), DateTimeKind.Utc),
        IsParticipant = !reader.HasColumn("IsParticipant") || reader.GetBoolean("IsParticipant"),
        WinningUserId = !reader.HasColumn("WinningUserId") || reader.IsDBNull(reader.GetOrdinal("WinningUserId")) ? null : reader.GetGuid("WinningUserId"),
        WinningTeam = !reader.HasColumn("WinningTeam") || reader.IsDBNull(reader.GetOrdinal("WinningTeam")) ? null : reader.GetInt32("WinningTeam"),
        CaseKeys = reader.IsDBNull(reader.GetOrdinal("CaseKeys")) ? [] : JsonSerializer.Deserialize<List<string>>(reader.GetString("CaseKeys")) ?? []
    };
    private static CaseBattleTimingSettingsObj ReadTimingSettings(MySqlDataReader reader) => new()
    {
        MaxCasesPerBattle = reader.GetInt32("MaxCasesPerBattle"), ReadyPauseMs = reader.GetInt32("ReadyPauseMs"), ReadyCountdownMs = reader.GetInt32("ReadyCountdownMs"), PreSpinPauseMs = reader.HasColumn("PreSpinPauseMs") ? reader.GetInt32("PreSpinPauseMs") : 2500, SpinDurationMs = reader.GetInt32("SpinDurationMs"), LandedResultPauseMs = reader.HasColumn("LandedResultPauseMs") ? reader.GetInt32("LandedResultPauseMs") : 750, RoundRevealPauseMs = reader.GetInt32("RoundRevealPauseMs"), ResultsPauseMs = reader.GetInt32("ResultsPauseMs"), WinnerIntroPauseMs = reader.GetInt32("WinnerIntroPauseMs"), WinnerTallyDurationMs = reader.GetInt32("WinnerTallyDurationMs"), WinnerVerdictPauseMs = reader.GetInt32("WinnerVerdictPauseMs"), WinnerTransferDurationMs = reader.GetInt32("WinnerTransferDurationMs")
    };
    private static IEnumerable<MySqlParameter> Parameters(params (string Name, object? Value)[] items) => items.Select(item => new MySqlParameter(item.Name, item.Value ?? DBNull.Value));
}

internal static class CaseBattleReaderExtensions
{
    public static bool HasColumn(this MySqlDataReader reader, string name) { for (int i=0;i<reader.FieldCount;i++) if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase)) return true; return false; }
}
