using PersonalTools.Data.CaseBattles;
using PersonalTools.Data.CaseOpening;
using PersonalTools.Entities.CaseBattles;
using PersonalTools.Entities.CaseOpening;
using PersonalTools.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace PersonalTools.Classes.CaseBattles;

public interface ICaseBattleFuncs
{
    Task<CaseBattleSummaryObj> Create(Guid userId, CaseBattleCreateRequestObj request, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> Get(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj?> GetActive(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleDetailObj> GetDetail(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> Join(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> SetReady(Guid userId, Guid battleId, bool isReady, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> Start(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task Cancel(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task Leave(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleBuyAllResultObj> BuyAll(Guid userId, CaseBattleBuyAllRequestObj request, CancellationToken cancellationToken = default);
    Task<List<CaseBattleHistoryObj>> GetHistory(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleAdminReconciliationObj>> GetAdminReconciliation(CancellationToken cancellationToken = default);
    Task ReconcileExpired(Guid battleId, CancellationToken cancellationToken = default);
    Task CancelPendingAsAdmin(Guid battleId, CancellationToken cancellationToken = default);
    Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetInvitableUserUnlockedCases(Guid userId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<CaseBattleDetailObj> InviteParticipants(Guid userId, Guid battleId, CaseBattleInviteRequestObj request, CancellationToken cancellationToken = default);
    Task<List<CaseBattleInvitationObj>> GetPendingInvitations(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseBattlePendingCreatedObj>> GetPendingCreated(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> AcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> BuyMissingCasesAndAcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task DeclineInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default);
    Task<CaseBattleTimingSettingsObj> GetTimingSettings(CancellationToken cancellationToken = default);
    Task SetTimingSettings(CaseBattleTimingSettingsObj settings, CancellationToken cancellationToken = default);
    Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task SetFreeForAll3Enabled(bool enabled, CancellationToken cancellationToken = default);
    Task SetFreeForAll4Enabled(bool enabled, CancellationToken cancellationToken = default);
    Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default);
}

public sealed class CaseBattleFuncs(ICaseBattleData data, ICaseOpeningData caseOpeningData, ICaseOpeningReferenceData referenceData, IHubContext<CaseBattleHub> hub, IOptions<CaseBattleFeatureOptions> options, ILogger<CaseBattleFuncs> logger) : ICaseBattleFuncs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleSummaryObj> Create(Guid userId, CaseBattleCreateRequestObj request, CancellationToken cancellationToken = default)
    {

        await EnsureFeatureEnabled(cancellationToken);

        List<string> cases = NormaliseCases(request.CaseKeys);

        await ValidateMode(request.Mode, cases.Count, cancellationToken);

        List<Guid> invitedUserIds = NormaliseInvitedUsers(request);

        if (request.UseBot)
        {
            if (request.Mode is not (CaseBattleModes.Duel or CaseBattleModes.FreeForAll3 or CaseBattleModes.FreeForAll4)) 
                throw new InvalidOperationException("Battle Bot is unavailable for that battle mode.");

            if (invitedUserIds.Count != 0) 
                throw new InvalidOperationException("Choose Battle Bot or invited players, not both.");

            if (!(await data.GetBotStatus(cancellationToken)).Enabled) 
                throw new InvalidOperationException("Battle Bot is not currently available.");
        }
        else
        {
            ValidateInvitedUsers(userId, request.Mode, invitedUserIds);
        }

        if (!request.UseBot)
        {
            await ValidateOpponentUnlocks(userId, invitedUserIds, cases, cancellationToken);
        }

        await RequireOwnership(userId, cases, cancellationToken);

        Guid battleId = Guid.NewGuid();

        await data.Create(battleId, userId, request.Mode, cases, cancellationToken);

        if (request.UseBot)
        {
            try 
            { 
                await data.JoinBot(battleId, cancellationToken); 
            }
            catch
            {
                await data.Cancel(battleId, userId, CancellationToken.None);
                throw;
            }
            // Bot joins use the same waiting-room lifecycle as human battles. The join procedure
            // marks only the bot seat ready; the creator still confirms readiness in the lobby,
            // which then owns the shared countdown and idempotent start request.
        }
        else
        {
            try
            {
                foreach (Guid invitedUserId in invitedUserIds)
                {
                    await data.SetInvite(battleId, userId, invitedUserId, cancellationToken);
                }
            }
            catch
            {
                // Creation escrows cases in its own transaction. Unwind it if invitation
                // validation loses a race so a failed request cannot strand the escrow.
                await data.Cancel(battleId, userId, CancellationToken.None);
                throw;
            }
            foreach (Guid invitedUserId in invitedUserIds)
            {
                await PublishInvitation(invitedUserId, battleId, cancellationToken);
            }
        }

        logger.LogInformation("Case battle {BattleId} was created by {UserId}.", battleId, userId);

        return await Get(userId, battleId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<CaseBattleSummaryObj> Get(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        CaseBattleSummaryObj? battle = await data.Get(battleId, userId, cancellationToken);

        if (battle is null || !battle.IsParticipant) 
            throw new KeyNotFoundException("That battle is unavailable.");

        return battle;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CaseBattleSummaryObj?> GetActive(Guid userId, CancellationToken cancellationToken = default)
    {
        return data.GetActive(userId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<CaseBattleDetailObj> GetDetail(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        CaseBattleDetailObj? detail = await data.GetDetail(battleId, userId, cancellationToken);

        if (detail is null) 
            throw new KeyNotFoundException("That battle is unavailable.");

        detail.IsCreator = detail.CreatorUserId == userId;

        return detail;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleSummaryObj> Join(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Case battles can only be joined by accepting an invitation.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleDetailObj> InviteParticipants(Guid userId, Guid battleId, CaseBattleInviteRequestObj request, CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabled(cancellationToken);

        CaseBattleDetailObj battle = await GetDetail(userId, battleId, cancellationToken);

        if (!battle.IsCreator || battle.Status != "waiting") 
            throw new InvalidOperationException("Only the battle owner can invite players to a waiting battle.");

        List<Guid> invitedUserIds = (request.InvitedUserIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();

        if (invitedUserIds.Count == 0 || invitedUserIds.Any(id => id == userId)) 
            throw new InvalidOperationException("Choose another active user to invite.");

        await ValidateOpponentUnlocks(userId, invitedUserIds, battle.CaseKeys, cancellationToken);

        foreach (Guid invitedUserId in invitedUserIds)
        {
            await data.SetInvite(battleId, userId, invitedUserId, cancellationToken);
            await PublishInvitation(invitedUserId, battleId, cancellationToken);
        }

        await PublishChanged(battleId, "participants-invited", cancellationToken);

        return await GetDetail(userId, battleId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="isReady"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CaseBattleSummaryObj> SetReady(Guid userId, Guid battleId, bool isReady, CancellationToken cancellationToken = default)
    {
        await data.SetReady(battleId, userId, isReady, cancellationToken);

        CaseBattleSummaryObj result = await Get(userId, battleId, cancellationToken);

        await PublishChanged(battleId, "seat-ready", cancellationToken);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleSummaryObj> Start(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        // A retry after a dropped response or process restart is safe. Once the start transaction
        // committed, execution resumes from its persisted opening state instead of rolling again.
        CaseBattleSummaryObj current = await Get(userId, battleId, cancellationToken);

        if (current.Status == "waiting")
        {
            await data.Start(battleId, cancellationToken);
            await PublishChanged(battleId, "battle-started", cancellationToken);
        }
        else if (current.Status == "settled")
        {
            return current;
        }
        else if (current.Status != "opening")
        {
            throw new InvalidOperationException("This battle cannot be started.");
        }

        await ExecuteStartedBattle(userId, battleId, cancellationToken);

        CaseBattleSummaryObj result = await Get(userId, battleId, cancellationToken);

        await PublishChanged(battleId, "battle-settled", cancellationToken);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Cancel(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Cancel(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "battle-cancelled", cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Leave(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Leave(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "seat-left", cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CaseBattleHistoryObj>> GetHistory(Guid userId, CancellationToken cancellationToken = default)
    {
        return data.GetHistory(userId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default)
    {
        return data.GetInvitableUsers(userId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="invitedUserId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<List<string>> GetInvitableUserUnlockedCases(Guid userId, Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        if (!(await data.GetInvitableUsers(userId, cancellationToken)).Any(item => item.UserId == invitedUserId))
            throw new InvalidOperationException("That user is no longer available to invite.");

        return await caseOpeningData.GetCaseOpeningUnlockedCases(invitedUserId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default)
    {
        return data.GetBotStatus(cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<CaseBattleTimingSettingsObj> GetTimingSettings(CancellationToken cancellationToken = default)
    {
        return data.GetTimingSettings(cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SetTimingSettings(CaseBattleTimingSettingsObj settings, CancellationToken cancellationToken = default)
    {
        settings.MaxCasesPerBattle = ClampTiming(settings.MaxCasesPerBattle, 1, 50);
        settings.ReadyPauseMs = ClampTiming(settings.ReadyPauseMs, 0, 10000);
        settings.ReadyCountdownMs = ClampTiming(settings.ReadyCountdownMs, 0, 15000);
        settings.PreSpinPauseMs = ClampTiming(settings.PreSpinPauseMs, 0, 10000);
        settings.SpinDurationMs = ClampTiming(settings.SpinDurationMs, 3000, 5000);
        settings.LandedResultPauseMs = ClampTiming(settings.LandedResultPauseMs, 0, 5000);
        settings.RoundRevealPauseMs = ClampTiming(settings.RoundRevealPauseMs, 0, 10000);
        settings.ResultsPauseMs = ClampTiming(settings.ResultsPauseMs, 0, 10000);
        settings.WinnerIntroPauseMs = ClampTiming(settings.WinnerIntroPauseMs, 0, 10000);
        settings.WinnerTallyDurationMs = ClampTiming(settings.WinnerTallyDurationMs, 250, 20000);
        settings.WinnerVerdictPauseMs = ClampTiming(settings.WinnerVerdictPauseMs, 0, 10000);
        settings.WinnerTransferDurationMs = ClampTiming(settings.WinnerTransferDurationMs, 250, 20000);

        return data.SetTimingSettings(settings, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return data.SetFeatureEnabled(enabled, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SetFreeForAll3Enabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return data.SetFreeForAll3Enabled(enabled, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SetFreeForAll4Enabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return data.SetFreeForAll4Enabled(enabled, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return data.SetBotEnabled(enabled, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CaseBattleInvitationObj>> GetPendingInvitations(Guid userId, CancellationToken cancellationToken = default)
    {
        return data.GetPendingInvitations(userId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CaseBattlePendingCreatedObj>> GetPendingCreated(Guid userId, CancellationToken cancellationToken = default)
    {
        return data.GetPendingCreated(userId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<CaseBattleSummaryObj> AcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.AcceptInvite(battleId, userId, cancellationToken);

        CaseBattleSummaryObj result = await Get(userId, battleId, cancellationToken);

        await PublishChanged(battleId, "invite-accepted", cancellationToken);

        await PublishInvitation(result.CreatorUserId, battleId, cancellationToken);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleSummaryObj> BuyMissingCasesAndAcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabled(cancellationToken);

        if (!(await data.GetPendingInvitations(userId, cancellationToken)).Any(item => item.BattleId == battleId))
            throw new InvalidOperationException("This invitation has expired or is unavailable.");

        CaseBattleSummaryObj? invitation = await data.Get(battleId, userId, cancellationToken);

        if (invitation is null || invitation.Status != "waiting") 
            throw new InvalidOperationException("This invitation has expired or is unavailable.");

        Dictionary<string, int> owned = (await caseOpeningData.GetCaseOpeningOwnedCases(userId, cancellationToken)).ToDictionary(item => item.CaseKey, item => item.Quantity, StringComparer.OrdinalIgnoreCase);

        List<string> missingCases = invitation.CaseKeys.GroupBy(key => key, StringComparer.OrdinalIgnoreCase).SelectMany(group => Enumerable.Repeat(group.Key, Math.Max(0, group.Count() - (owned.TryGetValue(group.Key, out int quantity) ? quantity : 0)))).ToList();

        if (missingCases.Count > 0)
            await BuyAll(userId, new CaseBattleBuyAllRequestObj { CaseKeys = missingCases }, cancellationToken);

        return await AcceptInvite(userId, battleId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeclineInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.DeclineInvite(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "invite-declined", cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<CaseBattleAdminReconciliationObj>> GetAdminReconciliation(CancellationToken cancellationToken = default)
    {
        return data.GetAdminReconciliation(cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ReconcileExpired(Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Expire(battleId, cancellationToken);

        logger.LogInformation("Expired case battle {BattleId} was reconciled by an administrator.", battleId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CancelPendingAsAdmin(Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.CancelPendingAsAdmin(battleId, cancellationToken);

        await PublishChanged(battleId, "battle-cancelled", cancellationToken);

        logger.LogInformation("Pending case battle {BattleId} was cancelled by an administrator.", battleId);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<CaseBattleBuyAllResultObj> BuyAll(Guid userId, CaseBattleBuyAllRequestObj request, CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabled(cancellationToken);

        List<string> cases = NormaliseCases(request.CaseKeys);

        if (cases.Count is < 1 or > 500) 
            throw new InvalidOperationException("Choose between 1 and 500 cases to buy.");

        HashSet<string> unlockedCases = (await caseOpeningData.GetCaseOpeningUnlockedCases(userId, cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string? lockedCase = cases.FirstOrDefault(caseKey => !unlockedCases.Contains(caseKey));

        if (lockedCase is not null) 
            throw new InvalidOperationException($"Unlock {lockedCase} before buying it for a case battle.");

        Dictionary<string, CaseOpeningCaseSettingsObj> settings = (await caseOpeningData.GetCaseSettings(cancellationToken)).ToDictionary(item => item.CaseKey, StringComparer.OrdinalIgnoreCase);

        List<(string CaseKey, int Quantity, int CostStars, long CostGbpPence)> purchases = cases.GroupBy(item => item, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            if (!settings.TryGetValue(group.Key, out CaseOpeningCaseSettingsObj? configured)) 
                throw new InvalidOperationException($"{group.Key} does not have a purchase price configured.");

            return (group.Key, group.Count(), configured.PurchaseCostStars, configured.PurchaseCostGbpPence);

        }).ToList();

        CaseBattleBuyAllResultObj? result = await data.BuyAll(userId, purchases, cancellationToken);

        return result ?? throw new InvalidOperationException("The required cases could not be purchased.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cases"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task RequireOwnership(Guid userId, List<string> cases, CancellationToken cancellationToken)
    {
        Dictionary<string, int> owned = (await caseOpeningData.GetCaseOpeningOwnedCases(userId, cancellationToken)).ToDictionary(x => x.CaseKey, x => x.Quantity, StringComparer.OrdinalIgnoreCase);

        string? missing = cases.GroupBy(key => key, StringComparer.OrdinalIgnoreCase).Select(group => new { CaseKey = group.Key, Required = group.Count() }).FirstOrDefault(item => !owned.TryGetValue(item.CaseKey, out int quantity) || quantity < item.Required)?.CaseKey;
        
        if (missing is not null) 
            throw new InvalidOperationException($"You need to own {missing} before joining this battle. Use Buy all first if you have enough inventory space.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureFeatureEnabled(CancellationToken cancellationToken)
    {
        // Config is an emergency deployment brake; day-to-day visibility is managed by administrators.
        if (!options.Value.Enabled || !(await data.GetBotStatus(cancellationToken)).CaseBattlesEnabled)
            throw new InvalidOperationException("Case battles are temporarily unavailable.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="raw"></param>
    /// <returns></returns>
    private static List<string> NormaliseCases(IEnumerable<string>? raw)
    {
        return (raw ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private static List<Guid> NormaliseInvitedUsers(CaseBattleCreateRequestObj request)
    {
        IEnumerable<Guid> supplied = request.InvitedUserIds ?? [];

        if (request.InvitedUserId is Guid legacyId && legacyId != Guid.Empty) 
            supplied = supplied.Append(legacyId);

        return supplied.Where(id => id != Guid.Empty).Distinct().ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ownerUserId"></param>
    /// <param name="mode"></param>
    /// <param name="invitedUserIds"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void ValidateInvitedUsers(Guid ownerUserId, string mode, List<Guid> invitedUserIds)
    {
        int requiredOpponents = CaseBattleModes.PlayerCount(mode) - 1;

        if (requiredOpponents < 1 || invitedUserIds.Count != requiredOpponents || invitedUserIds.Any(id => id == ownerUserId))
            throw new InvalidOperationException($"Choose exactly {requiredOpponents} other player{(requiredOpponents == 1 ? string.Empty : "s")} to invite.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ownerUserId"></param>
    /// <param name="invitedUserIds"></param>
    /// <param name="cases"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ValidateOpponentUnlocks(Guid ownerUserId, IEnumerable<Guid> invitedUserIds, List<string> cases, CancellationToken cancellationToken)
    {
        foreach (Guid invitedUserId in invitedUserIds)
        {
            HashSet<string> unlockedCases = (await GetInvitableUserUnlockedCases(ownerUserId, invitedUserId, cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            string? unavailableCase = cases.FirstOrDefault(caseKey => !unlockedCases.Contains(caseKey));

            if (unavailableCase is not null) 
                throw new InvalidOperationException($"An invited player has not unlocked {unavailableCase}.");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <returns></returns>
    private static int ClampTiming(int value, int minimum, int maximum)
    {
        return Math.Clamp(value, minimum, maximum);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="caseCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ValidateMode(string? mode, int caseCount, CancellationToken cancellationToken)
    {
        if (CaseBattleModes.PlayerCount(mode ?? string.Empty) == 0) 
            throw new InvalidOperationException("Choose a supported battle mode.");

        if (!CaseBattleModes.IsEnabled(mode ?? string.Empty)) 
            throw new InvalidOperationException("That case battle mode is not available.");

        if (mode == CaseBattleModes.FreeForAll3 && !(await data.GetBotStatus(cancellationToken)).FreeForAll3Enabled) 
            throw new InvalidOperationException("1v1v1 case battles are currently disabled.");

        if (mode == CaseBattleModes.FreeForAll4 && !(await data.GetBotStatus(cancellationToken)).FreeForAll4Enabled) 
            throw new InvalidOperationException("1v1v1v1 case battles are currently disabled.");

        int maximum = (await data.GetTimingSettings(cancellationToken)).MaxCasesPerBattle;

        if (caseCount < 1 || caseCount > maximum) 
            throw new InvalidOperationException($"Choose between 1 and {maximum} cases.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ExecuteStartedBattle(Guid userId, Guid battleId, CancellationToken cancellationToken)
    {
        List<CaseBattleRollPlanObj> plan = await data.GetExecutionPlan(battleId, userId, cancellationToken);

        if (plan.Count == 0 || plan.Any(item => item.Status != "opening" || !CaseBattleModes.IsEnabled(item.Mode)))
            throw new InvalidOperationException("This battle cannot be executed.");

        HashSet<string> lockedMarketHashes = await data.GetLockedMarketHashes(battleId, userId, cancellationToken);

        if (lockedMarketHashes.Count == 0) 
            throw new InvalidOperationException("The locked price snapshot has no market prices.");

        Dictionary<string, CaseOpeningCaseObj> cases = new(StringComparer.OrdinalIgnoreCase);

        foreach (string caseKey in plan.Select(item => item.CaseKey).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cases[caseKey] = await referenceData.GetCase(caseKey, cancellationToken);
        }

        List<CaseBattleServerRollObj> rolls = plan.Select(item => Roll(item, cases[item.CaseKey], lockedMarketHashes)).ToList();

        await data.StageRolls(battleId, rolls, cancellationToken);

        foreach (int round in rolls.Select(item => item.RoundNumber).Distinct().OrderBy(value => value))
        {
            await PublishChanged(battleId, $"round-resolved:{round}", cancellationToken);
        }

        await data.Settle(battleId, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="battleId"></param>
    /// <param name="reason"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private Task PublishChanged(Guid battleId, string reason, CancellationToken cancellationToken)
    {
        return hub.Clients.Group(CaseBattleHub.Group(battleId)).SendAsync(CaseBattleHub.EventChanged, new { battleId, reason }, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="battleId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private Task PublishInvitation(Guid userId, Guid battleId, CancellationToken cancellationToken)
    {
        return hub.Clients.Group(CaseBattleHub.UserGroup(userId)).SendAsync(CaseBattleHub.EventInvitation, new { battleId }, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plan"></param>
    /// <param name="caseData"></param>
    /// <param name="lockedMarketHashes"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static CaseBattleServerRollObj Roll(CaseBattleRollPlanObj plan, CaseOpeningCaseObj caseData, HashSet<string> lockedMarketHashes)
    {
        List<CaseOpeningOddsObj> odds = caseData.Odds.Where(odd => caseData.Items.Any(item => item.RarityKey == odd.RarityKey)).ToList();

        if (odds.Count == 0) 
            throw new InvalidOperationException("The battle case contents could not be loaded.");

        // A partial snapshot must never silently use a live price. Retry a fresh cryptographic
        // roll until it resolves to an item that exists in the snapshot, then fail closed.
        for (int attempt = 0; attempt < 256; attempt++)
        {
            string rarity = SelectRarity(odds);

            List<CaseOpeningItemObj> eligible = caseData.Items.Where(item => item.RarityKey == rarity).ToList();

            if (eligible.Count == 0)
                continue;

            CaseOpeningItemObj item = eligible[RandomNumberGenerator.GetInt32(eligible.Count)];

            (string marketHashName, string wear, bool statTrak) = BuildMarketVariant(item, caseData.Type);

            if (!lockedMarketHashes.Contains(marketHashName)) 
                continue;

            return new CaseBattleServerRollObj
            {
                OriginalOwnerUserId = plan.UserId,
                RoundNumber = plan.RoundNumber,
                OpeningId = Guid.NewGuid(),
                CaseKey = plan.CaseKey,
                SourceItemId = item.SourceItemId,
                ItemName = item.Name,
                MarketHashName = marketHashName,
                ImageUrl = item.ImageUrl,
                RarityKey = item.RarityKey,
                RarityName = item.RarityName,
                RarityColor = item.RarityColor,
                Wear = wear,
                IsStatTrak = statTrak,
                IsRareSpecial = item.IsRareSpecial,
                SupportsStatTrak = item.SupportsStatTrak
            };
        }

        throw new InvalidOperationException("The locked price snapshot is incomplete for one of the selected cases.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="odds"></param>
    /// <returns></returns>
    private static string SelectRarity(List<CaseOpeningOddsObj> odds)
    {
        List<(CaseOpeningOddsObj Odd, int Weight)> weighted = odds.Select(odd => (odd, Math.Max(0, (int)(odd.Percentage * 10_000m)))).Where(item => item.Item2 > 0).ToList();

        int roll = RandomNumberGenerator.GetInt32(weighted.Sum(item => item.Weight));
        int boundary = 0;

        foreach ((CaseOpeningOddsObj odd, int weight) in weighted)
        {
            boundary += weight;
            if (roll < boundary) return odd.RarityKey;
        }

        return weighted[^1].Odd.RarityKey;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="caseType"></param>
    /// <returns></returns>
    private static (string MarketHashName, string Wear, bool StatTrak) BuildMarketVariant(CaseOpeningItemObj item, string caseType)
    {
        if (caseType.Equals("Sticker Capsule", StringComparison.OrdinalIgnoreCase))
            return ($"Sticker | {item.Name}", string.Empty, false);

        decimal minimum = item.MinFloat ?? 0m;
        decimal maximum = Math.Max(minimum, item.MaxFloat ?? 1m);
        decimal value = decimal.Round(minimum + ((maximum - minimum) * (RandomNumberGenerator.GetInt32(1_000_001) / 1_000_000m)), 6);
        string wear = value < .07m ? "Factory New" : value < .15m ? "Minimal Wear" : value < .38m ? "Field-Tested" : value < .45m ? "Well-Worn" : "Battle-Scarred";
        bool souvenir = caseType.Equals("Souvenir Package", StringComparison.OrdinalIgnoreCase) || caseType.Equals("Souvenir", StringComparison.OrdinalIgnoreCase);
        bool statTrak = !souvenir && item.SupportsStatTrak && RandomNumberGenerator.GetInt32(100) < 10;
        string prefix = souvenir ? "Souvenir " : $"{(item.IsRareSpecial ? "★ " : string.Empty)}{(statTrak ? "StatTrak™ " : string.Empty)}";

        return ($"{prefix}{item.Name} ({wear})", wear, statTrak);
    }
}
