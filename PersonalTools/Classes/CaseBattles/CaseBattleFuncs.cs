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
    Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleInvitationObj?> GetPendingInvitation(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseBattleSummaryObj> AcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task DeclineInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default);
    Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default);
    Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default);
}

public sealed class CaseBattleFuncs(
    ICaseBattleData data,
    ICaseOpeningData caseOpeningData,
    ICaseOpeningReferenceData referenceData,
    IHubContext<CaseBattleHub> hub,
    IOptions<CaseBattleFeatureOptions> options,
    ILogger<CaseBattleFuncs> logger) : ICaseBattleFuncs
{
    public async Task<CaseBattleSummaryObj> Create(Guid userId, CaseBattleCreateRequestObj request, CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabled(cancellationToken);
        if (!request.UseBot && (!request.InvitedUserId.HasValue || request.InvitedUserId.Value == Guid.Empty || request.InvitedUserId.Value == userId)) throw new InvalidOperationException("Choose another active user to invite.");
        if (request.UseBot && !(await data.GetBotStatus(cancellationToken)).Enabled) throw new InvalidOperationException("Battle Bot is not currently available.");
        List<string> cases = NormaliseCases(request.CaseKeys);
        ValidateMode(request.Mode, cases.Count);
        await RequireOwnership(userId, cases, cancellationToken);
        Guid battleId = Guid.NewGuid();
        await data.Create(battleId, userId, request.Mode, cases, cancellationToken);
        if (request.UseBot)
        {
            await data.JoinBot(battleId, cancellationToken);
            await SetReady(userId, battleId, true, cancellationToken);
            await Start(userId, battleId, cancellationToken);
        }
        else
        {
            await data.SetInvite(battleId, userId, request.InvitedUserId!.Value, cancellationToken);
            await PublishInvitation(request.InvitedUserId.Value, battleId, cancellationToken);
        }
        logger.LogInformation("Case battle {BattleId} was created by {UserId}.", battleId, userId);
        return await Get(userId, battleId, cancellationToken);
    }

    public async Task<CaseBattleSummaryObj> Get(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        CaseBattleSummaryObj? battle = await data.Get(battleId, userId, cancellationToken);
        if (battle is null || !battle.IsParticipant) throw new KeyNotFoundException("That battle is unavailable.");
        return battle;
    }

    public Task<CaseBattleSummaryObj?> GetActive(Guid userId, CancellationToken cancellationToken = default) => data.GetActive(userId, cancellationToken);
    public async Task<CaseBattleDetailObj> GetDetail(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        CaseBattleDetailObj? detail = await data.GetDetail(battleId, userId, cancellationToken);
        if (detail is null) throw new KeyNotFoundException("That battle is unavailable.");
        detail.IsCreator = detail.CreatorUserId == userId;
        return detail;
    }

    public async Task<CaseBattleSummaryObj> Join(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Case battles can only be joined by accepting an invitation.");
    }
    public async Task<CaseBattleSummaryObj> SetReady(Guid userId, Guid battleId, bool isReady, CancellationToken cancellationToken = default)
    {
        await data.SetReady(battleId,userId,isReady,cancellationToken);
        CaseBattleSummaryObj result = await Get(userId,battleId,cancellationToken);
        await PublishChanged(battleId, "seat-ready", cancellationToken);
        return result;
    }

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

        await ExecuteStartedDuel(userId, battleId, cancellationToken);
        CaseBattleSummaryObj result = await Get(userId, battleId, cancellationToken);
        await PublishChanged(battleId, "battle-settled", cancellationToken);
        return result;
    }

    public async Task Cancel(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Cancel(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "battle-cancelled", cancellationToken);
    }

    public async Task Leave(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Leave(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "seat-left", cancellationToken);
    }

    public Task<List<CaseBattleHistoryObj>> GetHistory(Guid userId, CancellationToken cancellationToken = default) => data.GetHistory(userId, cancellationToken);
    public Task<List<CaseBattleInvitableUserObj>> GetInvitableUsers(Guid userId, CancellationToken cancellationToken = default) => data.GetInvitableUsers(userId, cancellationToken);
    public Task<CaseBattleBotStatusObj> GetBotStatus(CancellationToken cancellationToken = default) => data.GetBotStatus(cancellationToken);
    public Task SetFeatureEnabled(bool enabled, CancellationToken cancellationToken = default) => data.SetFeatureEnabled(enabled, cancellationToken);
    public Task SetBotEnabled(bool enabled, CancellationToken cancellationToken = default) => data.SetBotEnabled(enabled, cancellationToken);
    public Task<CaseBattleInvitationObj?> GetPendingInvitation(Guid userId, CancellationToken cancellationToken = default) => data.GetPendingInvitation(userId, cancellationToken);
    public async Task<CaseBattleSummaryObj> AcceptInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.AcceptInvite(battleId, userId, cancellationToken);
        CaseBattleSummaryObj result = await Get(userId, battleId, cancellationToken);
        await PublishChanged(battleId, "invite-accepted", cancellationToken);
        await PublishInvitation(result.CreatorUserId, battleId, cancellationToken);
        return result;
    }
    public async Task DeclineInvite(Guid userId, Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.DeclineInvite(battleId, userId, cancellationToken);
        await PublishChanged(battleId, "invite-declined", cancellationToken);
    }
    public Task<List<CaseBattleAdminReconciliationObj>> GetAdminReconciliation(CancellationToken cancellationToken = default) => data.GetAdminReconciliation(cancellationToken);
    public async Task ReconcileExpired(Guid battleId, CancellationToken cancellationToken = default)
    {
        await data.Expire(battleId, cancellationToken);
        logger.LogInformation("Expired case battle {BattleId} was reconciled by an administrator.", battleId);
    }

    public async Task<CaseBattleBuyAllResultObj> BuyAll(Guid userId, CaseBattleBuyAllRequestObj request, CancellationToken cancellationToken = default)
    {
        await EnsureFeatureEnabled(cancellationToken);
        List<string> cases = NormaliseCases(request.CaseKeys);
        if (cases.Count is < 1 or > 500) throw new InvalidOperationException("Choose between 1 and 500 cases to buy.");
        Dictionary<string, CaseOpeningCaseSettingsObj> settings = (await caseOpeningData.GetCaseSettings(cancellationToken)).ToDictionary(item => item.CaseKey, StringComparer.OrdinalIgnoreCase);
        List<(string CaseKey, int Quantity, int CostStars, long CostGbpPence)> purchases = cases.GroupBy(item => item, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            if (!settings.TryGetValue(group.Key, out CaseOpeningCaseSettingsObj? configured)) throw new InvalidOperationException($"{group.Key} does not have a purchase price configured.");
            return (group.Key, group.Count(), configured.PurchaseCostStars, configured.PurchaseCostGbpPence);
        }).ToList();
        CaseBattleBuyAllResultObj? result = await data.BuyAll(userId, purchases, cancellationToken);
        return result ?? throw new InvalidOperationException("The required cases could not be purchased.");
    }

    private async Task RequireOwnership(Guid userId, List<string> cases, CancellationToken cancellationToken)
    {
        Dictionary<string, int> owned = (await caseOpeningData.GetCaseOpeningOwnedCases(userId, cancellationToken)).ToDictionary(x => x.CaseKey, x => x.Quantity, StringComparer.OrdinalIgnoreCase);
        string? missing = cases.GroupBy(key => key, StringComparer.OrdinalIgnoreCase).Select(group => new { CaseKey = group.Key, Required = group.Count() })
            .FirstOrDefault(item => !owned.TryGetValue(item.CaseKey, out int quantity) || quantity < item.Required)?.CaseKey;
        if (missing is not null) throw new InvalidOperationException($"You need to own {missing} before joining this battle. Use Buy all first if you have enough inventory space.");
    }
    private async Task EnsureFeatureEnabled(CancellationToken cancellationToken)
    {
        // Config is an emergency deployment brake; day-to-day visibility is managed by administrators.
        if (!options.Value.Enabled || !(await data.GetBotStatus(cancellationToken)).CaseBattlesEnabled)
            throw new InvalidOperationException("Case battles are temporarily unavailable.");
    }
    private static List<string> NormaliseCases(IEnumerable<string>? raw) => (raw ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).ToList();
    private static void ValidateMode(string? mode, int caseCount)
    {
        if (CaseBattleModes.PlayerCount(mode ?? string.Empty) == 0) throw new InvalidOperationException("Choose a supported battle mode.");
        if (!CaseBattleModes.IsEnabled(mode ?? string.Empty)) throw new InvalidOperationException("Only 1v1 case battles are available during the initial rollout.");
        if (caseCount is < 1 or > 20) throw new InvalidOperationException("Choose between 1 and 20 cases.");
    }

    private async Task ExecuteStartedDuel(Guid userId, Guid battleId, CancellationToken cancellationToken)
    {
        List<CaseBattleRollPlanObj> plan = await data.GetExecutionPlan(battleId, userId, cancellationToken);
        if (plan.Count == 0 || plan.Any(item => item.Status != "opening" || item.Mode != CaseBattleModes.Duel))
        {
            throw new InvalidOperationException("This battle cannot be executed.");
        }

        HashSet<string> lockedMarketHashes = await data.GetLockedMarketHashes(battleId, userId, cancellationToken);
        if (lockedMarketHashes.Count == 0) throw new InvalidOperationException("The locked price snapshot has no market prices.");
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

    private Task PublishChanged(Guid battleId, string reason, CancellationToken cancellationToken) =>
        hub.Clients.Group(CaseBattleHub.Group(battleId)).SendAsync(CaseBattleHub.EventChanged, new { battleId, reason }, cancellationToken);
    private Task PublishInvitation(Guid userId, Guid battleId, CancellationToken cancellationToken) =>
        hub.Clients.Group(CaseBattleHub.UserGroup(userId)).SendAsync(CaseBattleHub.EventInvitation, new { battleId }, cancellationToken);

    private static CaseBattleServerRollObj Roll(
        CaseBattleRollPlanObj plan,
        CaseOpeningCaseObj caseData,
        HashSet<string> lockedMarketHashes)
    {
        List<CaseOpeningOddsObj> odds = caseData.Odds
            .Where(odd => caseData.Items.Any(item => item.RarityKey == odd.RarityKey))
            .ToList();
        if (odds.Count == 0) throw new InvalidOperationException("The battle case contents could not be loaded.");

        // A partial snapshot must never silently use a live price. Retry a fresh cryptographic
        // roll until it resolves to an item that exists in the snapshot, then fail closed.
        for (int attempt = 0; attempt < 256; attempt++)
        {
            string rarity = SelectRarity(odds);
            List<CaseOpeningItemObj> eligible = caseData.Items.Where(item => item.RarityKey == rarity).ToList();
            if (eligible.Count == 0) continue;
            CaseOpeningItemObj item = eligible[RandomNumberGenerator.GetInt32(eligible.Count)];
            (string marketHashName, string wear, bool statTrak) = BuildMarketVariant(item, caseData.Type);
            if (!lockedMarketHashes.Contains(marketHashName)) continue;

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

    private static string SelectRarity(List<CaseOpeningOddsObj> odds)
    {
        List<(CaseOpeningOddsObj Odd, int Weight)> weighted = odds
            .Select(odd => (odd, Math.Max(0, (int)(odd.Percentage * 10_000m))))
            .Where(item => item.Item2 > 0)
            .ToList();
        int roll = RandomNumberGenerator.GetInt32(weighted.Sum(item => item.Weight));
        int boundary = 0;
        foreach ((CaseOpeningOddsObj odd, int weight) in weighted)
        {
            boundary += weight;
            if (roll < boundary) return odd.RarityKey;
        }

        return weighted[^1].Odd.RarityKey;
    }

    private static (string MarketHashName, string Wear, bool StatTrak) BuildMarketVariant(CaseOpeningItemObj item, string caseType)
    {
        if (caseType.Equals("Sticker Capsule", StringComparison.OrdinalIgnoreCase))
        {
            return ($"Sticker | {item.Name}", string.Empty, false);
        }

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
