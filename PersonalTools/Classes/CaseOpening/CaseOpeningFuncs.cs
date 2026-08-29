using System.Security.Cryptography;
using System.Text.Json;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using PersonalTools.Classes;
using PersonalTools.Entities;
using PersonalTools.Data.CaseOpening;
using PersonalTools.Entities.CaseOpening;
using PersonalTools.Hubs;

namespace PersonalTools.Classes.CaseOpening;

public interface ICaseOpeningFuncs
{
    Task<List<CaseOpeningCaseSummaryObj>> GetCaseOpeningCases(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningCaseObj> GetCaseOpeningCase(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningHistoryObj>> GetCaseOpeningHistory(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningCollectionObj> GetCaseOpeningCollection(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCollectionSummaryObj>> GetCaseOpeningCollections(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> GetCaseOpeningBotProgress(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> PurchaseCaseOpeningBotServer(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> PurchaseCaseOpeningBot(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> UpgradeCaseOpeningBotServer(Guid userId, Guid serverId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> UpgradeCaseOpeningBot(Guid userId, Guid botId, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotProgressObj> SetCaseOpeningBotServerEnabled(Guid userId, Guid serverId, bool isEnabled, CancellationToken cancellationToken = default);
    Task<CaseOpeningResultObj> OpenCaseWithBot(Guid userId, Guid botId, string caseKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningBotCycleResultObj> RunCaseOpeningBotCycle(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> GetCaseOpeningProgress(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> ClaimCaseOpeningDailyDrop(Guid userId, List<string> rewardKeys, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> UnlockCaseOpeningDailyDropUpgrade(Guid userId, string upgradeKey, CancellationToken cancellationToken = default);
    Task<int> GetDailyDropRequiredXp(CancellationToken cancellationToken = default);
    Task SetDailyDropRequiredXp(int requiredXp, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> ResetDailyDrop(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryCapacityObj> GetCaseOpeningInventoryCapacity(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningPlayerStatsObj> GetCaseOpeningPlayerStats(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningAchievementSummaryObj> GetCaseOpeningAchievements(Guid userId, CancellationToken cancellationToken = default);
    Task RecordCaseOpeningLogin(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> UnlockCaseOpeningCase(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningCasePurchaseResultObj> PurchaseCaseOpeningCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default);
    Task<CaseOpeningCaseDiscardResultObj> DiscardCaseOpeningCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default);
    Task<CaseOpeningStoragePurchaseResultObj> PurchaseCaseOpeningStorageContainer(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> UnlockCaseOpeningUpgrade(Guid userId, string upgradeKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningSellResultObj> SellCaseOpeningInventory(Guid userId, List<Guid> openingIds, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryLockObj> SetCaseOpeningInventoryLock(Guid userId, Guid openingId, bool isLocked, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryUpgradeObj> GetCaseOpeningInventoryUpgrades(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryUpgradeObj> UnlockCaseOpeningInventoryUpgrade(Guid userId, string upgradeKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryUpgradeObj> SetCaseOpeningAutoSellPreference(Guid userId, string rarityKey, bool enabled, bool? preserveStatTrak, CancellationToken cancellationToken = default);
    Task<CaseOpeningAutoBuySummaryObj> GetCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningAutoBuySummaryObj> SetCaseOpeningAutoBuyRule(Guid userId, string caseKey, CaseOpeningAutoBuyRuleRequestObj request, CancellationToken cancellationToken = default);
    Task<CaseOpeningAutoBuySummaryObj> DeleteCaseOpeningAutoBuyRule(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCasePurchaseResultObj>> EvaluateCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpResultObj> CreateCaseOpeningTradeUp(Guid userId, List<Guid> openingIds, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> GetCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> CreateCaseOpeningTradeUpRecipe(Guid userId, CaseOpeningTradeUpRecipeRequestObj request, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> SetCaseOpeningTradeUpRecipeActive(Guid userId, Guid recipeId, bool isActive, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> DeleteCaseOpeningTradeUpRecipe(Guid userId, Guid recipeId, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> CollectCaseOpeningTradeUpHolding(Guid userId, Guid holdingId, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryUpgradeObj> UpgradeCaseOpeningTradeUpRecipeSlots(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningTradeUpRecipeSummaryObj> UpgradeCaseOpeningTradeUpRecipeHolding(Guid userId, Guid recipeId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningTradeUpResultObj>> EvaluateCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningOpenBatchResultObj> OpenCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default);
    Task<CaseOpeningStatisticsObj> GetCaseOpeningStatistics(Guid userId, string caseKey, CancellationToken cancellationToken = default);

    // Game settings (global, shared) + per-case settings, for the variable-tweak modal.
    Task<CaseOpeningGameSettingsObj> GetGameSettings(CancellationToken cancellationToken = default);
    Task<CaseOpeningGameSettingsObj> SetGameSettings(CaseOpeningGameSettingsObj settings, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCaseSettingsObj>> GetCaseSettings(CancellationToken cancellationToken = default);
    Task SetCaseSettings(string caseKey, int tier, int unlockCostStars, long unlockCostGbpPence, int purchaseCostStars, long purchaseCostGbpPence, int xpRequirement, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningXpByRarityObj>> GetXpByRarity(CancellationToken cancellationToken = default);
    Task SetXpByRarity(string rarityKey, int xpAwarded, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningUpgradeDefinitionObj>> GetInventoryUpgradeSettings(CancellationToken cancellationToken = default);
    Task SetInventoryUpgradeSettings(string upgradeKey, int costStars, long costGbpPence, int requiredLevel, CancellationToken cancellationToken = default);
    Task<CaseOpeningPriceSnapshotSummaryObj> GetPriceSnapshots(CancellationToken cancellationToken = default);
    Task<CaseOpeningPriceSnapshotSummaryObj> CreatePriceSnapshot(CancellationToken cancellationToken = default);
    Task<CaseOpeningPriceSnapshotSummaryObj> ActivatePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<CaseOpeningPriceSnapshotSummaryObj> DeletePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<CaseOpeningPriceSnapshotSummaryObj> PublishPriceSnapshotBalance(CancellationToken cancellationToken = default);
    Task<CaseOpeningSpecialVariantAdminSummaryObj> GetSpecialVariantSettings(CancellationToken cancellationToken = default);
    Task<CaseOpeningSpecialVariantAdminSummaryObj> SaveSpecialVariantRule(Guid? ruleId, CaseOpeningSpecialVariantRuleRequestObj rule, CancellationToken cancellationToken = default);
    Task<CaseOpeningSpecialVariantAdminSummaryObj> CreateSpecialVariantPriceSnapshot(CaseOpeningSpecialVariantPriceSnapshotRequestObj request, CancellationToken cancellationToken = default);
    Task<CaseOpeningSpecialVariantAdminSummaryObj> ActivateSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<CaseOpeningSpecialVariantAdminSummaryObj> DeleteSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSpecialVariantListingEvidenceObj>> ImportSpecialVariantListings(Guid userId, CancellationToken cancellationToken = default);

    // Testing overrides for the caller's own account only.
    Task<CaseOpeningProgressObj> SetDevProgress(Guid userId, int stars, long gbpPence, int xp, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> SetDevUpgrades(Guid userId, bool skipAnimationUnlocked, int multiOpenLevel, int openSpeedLevel, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> SetDevCaseUnlock(Guid userId, string caseKey, bool unlock, CancellationToken cancellationToken = default);
    Task<CaseOpeningDevDropSettingsObj> GetDevDropSettings(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningDevDropSettingsObj> SetDevDropSettings(Guid userId, IEnumerable<string>? rarityGroups, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressObj> ResetDevProgress(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class CaseOpeningFuncs : ICaseOpeningFuncs
{
    private const int DailyDropRequiredXp = 100;
    private const int BotServerCapacity = 4;
    private const int MaximumIndividualBotSpeedLevel = 5;
    private const int BotSpeedUpgradeBaseCost = 300;
    private const int BotSpeedUpgradeIncrement = 100;
    private const int MaximumTradeUpRecipeSlots = 20;
    private const int MaximumTradeUpRecipeHoldingCapacity = 20;
    private const string StarterCaseKey = "kilowatt";

    private static readonly IReadOnlyDictionary<string, string[]> DevDropRarityKeys = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["blue"] = ["mil-spec", "high-grade"],
        ["purple"] = ["restricted", "remarkable"],
        ["pink"] = ["classified", "exotic"],
        ["red"] = ["covert"],
        ["gold"] = ["rare-special"]
    };

    // Higher unlock tiers pay more when their simulated items are sold. This does not depend on
    // rebalance-in-testing the same way Stars costs/XP requirements do, so it stays a plain const.
    private static readonly Dictionary<string, int> SaleValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mil-spec"] = 1,
        ["high-grade"] = 1,
        ["restricted"] = 2,
        ["remarkable"] = 2,
        ["classified"] = 4,
        ["exotic"] = 4,
        ["covert"] = 8,
        ["rare-special"] = 16
    };

    // Only weapon finishes can enter a contract. Sticker capsules and souvenir packages have
    // different item ladders, so treating them as normal weapon collections would be misleading.
    private static readonly Dictionary<string, string> TradeUpRarityLadder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mil-spec"] = "restricted",
        ["restricted"] = "classified",
        ["classified"] = "covert"
    };

    // Names must match WearFromFloat's output exactly - these are compared against a recipe's
    // accepted wears when a rolled output is checked for a match.
    private static readonly HashSet<string> KnownWears = new(StringComparer.Ordinal)
    {
        "Factory New", "Minimal Wear", "Field-Tested", "Well-Worn", "Battle-Scarred"
    };

    private readonly ICaseOpeningReferenceData _referenceData;
    private readonly ICaseOpeningData _data;
    private readonly ICS2ItemPriceData _prices;
    private readonly ILogger<CaseOpeningFuncs> _logger;
    private readonly IAppSettingsFuncs _settings;
    private readonly ICSFloatMarketData _csFloat;
    private readonly IHubContext<LiveWinnersHub> _liveWinners;

    public CaseOpeningFuncs(
        ICaseOpeningReferenceData referenceData,
        ICaseOpeningData data,
        ICS2ItemPriceData prices, IAppSettingsFuncs settings, ICSFloatMarketData csFloat, IHubContext<LiveWinnersHub> liveWinners,
        ILogger<CaseOpeningFuncs> logger)
    {
        _referenceData = referenceData;
        _data = data;
        _prices = prices;
        _liveWinners = liveWinners;
        _settings = settings;
        _csFloat = csFloat;
        _logger = logger;
    }

    public async Task<CaseOpeningCaseObj> GetCaseOpeningCase(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        CaseOpeningCaseObj caseData = await _referenceData.GetCase(caseKey, cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> settingsByKey = await GetCaseSettingsByKey(cancellationToken);
        if (settingsByKey.TryGetValue(caseKey, out CaseOpeningCaseSettingsObj? caseSettings))
        {
            caseData.UnlockCostStars = caseSettings.UnlockCostStars;
            caseData.PurchaseCostStars = caseSettings.PurchaseCostStars;
            caseData.UnlockCostGbpPence = caseSettings.UnlockCostGbpPence;
            caseData.PurchaseCostGbpPence = caseSettings.PurchaseCostGbpPence;
            caseData.Tier = caseSettings.Tier;
            caseData.XpRequirement = caseSettings.XpRequirement;
        }
        ApplyActiveCaseCosts(caseData, (await _data.GetGameSettings(cancellationToken)).EconomyMode);
        CaseOpeningOwnedCaseDbModel? ownedCase = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .FirstOrDefault(item => item.CaseKey.Equals(caseKey, StringComparison.OrdinalIgnoreCase));
        caseData.OwnedQuantity = ownedCase?.Quantity ?? 0;
        return caseData;
    }

    public async Task<List<CaseOpeningCaseSummaryObj>> GetCaseOpeningCases(Guid userId, CancellationToken cancellationToken = default)
    {
        // The selector only needs identity and artwork. Full contents stay behind the selected-case
        // endpoint so opening the page does not transfer every skin across all curated cases.
        List<CaseOpeningCaseObj> cases = await _referenceData.GetCuratedCases(cancellationToken);
        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        Dictionary<string, int> ownedQuantities = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .ToDictionary(item => item.CaseKey, item => item.Quantity, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, CaseOpeningCaseSettingsObj> caseSettings = await GetCaseSettingsByKey(cancellationToken);
        CaseOpeningGameSettingsObj gameSettings = await _data.GetGameSettings(cancellationToken);
        cases.ForEach(caseData =>
        {
            (int cost, int purchaseCost, int xpRequirement) = GetCaseSettings(caseSettings, caseData.CaseKey);
            caseData.UnlockCostStars = cost;
            caseData.PurchaseCostStars = purchaseCost;
            if (caseSettings.TryGetValue(caseData.CaseKey, out CaseOpeningCaseSettingsObj? configured))
            {
                caseData.UnlockCostGbpPence = configured.UnlockCostGbpPence;
                caseData.PurchaseCostGbpPence = configured.PurchaseCostGbpPence;
                caseData.Tier = configured.Tier;
            }
            caseData.XpRequirement = xpRequirement;
            caseData.SaleMultiplier = GetCaseSaleMultiplier(cost);
            caseData.IsUnlocked = unlockedCaseKeys.Contains(caseData.CaseKey, StringComparer.OrdinalIgnoreCase);
            ApplyActiveCaseCosts(caseData, gameSettings.EconomyMode);
            caseData.OwnedQuantity = ownedQuantities.GetValueOrDefault(caseData.CaseKey);
        });
        return cases
            .OrderBy(caseData => caseData.UnlockCostStars)
            .ThenBy(caseData => caseData.Name, StringComparer.OrdinalIgnoreCase)
            .Adapt<List<CaseOpeningCaseSummaryObj>>();
    }

    public async Task<List<CaseOpeningHistoryObj>> GetCaseOpeningHistory(Guid userId, CancellationToken cancellationToken = default)
    {
        List<CaseOpeningHistoryDbModel> history = await _data.GetCaseOpeningHistory(userId, cancellationToken);
        Dictionary<string, decimal> activePrices = (await _prices.GetActivePrices(cancellationToken))
            .ToDictionary(item => item.MarketHashName, item => item.Price, StringComparer.Ordinal);

        // Keep the database value as the historical pull-time estimate, but present the active
        // snapshot in the balance lab so switching snapshots compares the entire inventory fairly.
        foreach (CaseOpeningHistoryDbModel item in history)
        {
            item.EstimatedPrice = item.SpecialVariantPrice
                ?? (activePrices.TryGetValue(item.MarketHashName, out decimal price) ? price : null);
        }

        return history.Adapt<List<CaseOpeningHistoryObj>>();
    }

    public async Task<CaseOpeningCollectionObj> GetCaseOpeningCollection(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        CaseOpeningCaseObj caseData = await _referenceData.GetCase(caseKey, cancellationToken);
        List<CaseOpeningCollectionDbModel> collectedItems = await _data.GetCaseOpeningCollection(
            userId,
            caseKey,
            cancellationToken);
        return BuildCaseOpeningCollection(caseData, collectedItems);
    }

    public async Task<List<CaseOpeningCollectionSummaryObj>> GetCaseOpeningCollections(Guid userId, CancellationToken cancellationToken = default)
    {
        List<CaseOpeningCollectionDbModel> collectedItems = await _data.GetCaseOpeningCollections(userId, cancellationToken);
        if (collectedItems.Count == 0)
        {
            return [];
        }

        Dictionary<string, List<CaseOpeningCollectionDbModel>> collectedByCase = collectedItems
            .GroupBy(item => item.CaseKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        List<CaseOpeningCaseObj> cases = await _referenceData.GetCuratedCases(cancellationToken);
        List<CaseOpeningCollectionSummaryObj> summaries = [];

        foreach (CaseOpeningCaseObj caseData in cases.Where(item => collectedByCase.ContainsKey(item.CaseKey)))
        {
            List<CaseOpeningCollectionDbModel> caseCollectedItems = collectedByCase[caseData.CaseKey];
            CaseOpeningCollectionObj collection = BuildCaseOpeningCollection(caseData, caseCollectedItems);
            CaseOpeningCollectionSummaryObj summary = collection.Adapt<CaseOpeningCollectionSummaryObj>();
            summary.ImageUrl = caseData.ImageUrl;
            summary.FirstObtainedUtc = caseCollectedItems.Min(item => item.FirstObtainedUtc);
            summary.Rarities = collection.Items
                .GroupBy(item => new { item.RarityKey, item.RarityName, item.RarityColor })
                .OrderBy(group => GetCollectionRarityOrder(group.Key.RarityKey))
                .Select(group => new CaseOpeningCollectionRaritySummaryObj
                {
                    RarityKey = group.Key.RarityKey,
                    RarityName = group.Key.RarityName,
                    RarityColor = group.Key.RarityColor,
                    TotalItemCount = group.Count(),
                    CollectedItemCount = group.Count(item => item.IsCollected)
                })
                .ToList();
            summaries.Add(summary);
        }

        return summaries
            .OrderByDescending(summary => summary.FirstObtainedUtc)
            .ThenBy(summary => summary.CaseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static CaseOpeningCollectionObj BuildCaseOpeningCollection(CaseOpeningCaseObj caseData, List<CaseOpeningCollectionDbModel> collectedItems)
    {
        Dictionary<string, DateTime> firstObtainedBySourceId = collectedItems.ToDictionary(
            item => item.SourceItemId,
            item => item.FirstObtainedUtc,
            StringComparer.Ordinal);

        List<CaseOpeningCollectionItemObj> items = caseData.Items
            .Where(item => !item.IsRareSpecial)
            .Select(item =>
            {
                CaseOpeningCollectionItemObj collectionItem = item.Adapt<CaseOpeningCollectionItemObj>();
                if (firstObtainedBySourceId.TryGetValue(item.SourceItemId, out DateTime firstObtainedUtc))
                {
                    collectionItem.IsCollected = true;
                    collectionItem.FirstObtainedUtc = firstObtainedUtc;
                }

                return collectionItem;
            })
            .ToList();

        List<CaseOpeningItemObj> rareItems = caseData.Items
            .Where(item => item.IsRareSpecial)
            .ToList();

        // A case may contain many knife or glove finishes, but the collection is intended to
        // track rarity milestones. Any rare-special pull therefore completes one Gold objective.
        if (rareItems.Count > 0)
        {
            List<DateTime> rareFirstObtainedDates = rareItems
                .Where(item => firstObtainedBySourceId.ContainsKey(item.SourceItemId))
                .Select(item => firstObtainedBySourceId[item.SourceItemId])
                .ToList();

            CaseOpeningCollectionItemObj rareCollectionItem = rareItems[0]
                .Adapt<CaseOpeningCollectionItemObj>();
            rareCollectionItem.SourceItemId = $"{caseData.CaseKey}:rare-special";
            rareCollectionItem.Name = "Rare Special Item";
            rareCollectionItem.MarketHashName = string.Empty;
            rareCollectionItem.Description = "Pull any rare special item from this case to complete this objective.";
            rareCollectionItem.WeaponName = string.Empty;
            rareCollectionItem.PatternName = string.Empty;
            rareCollectionItem.PaintIndex = string.Empty;
            rareCollectionItem.Phase = string.Empty;
            rareCollectionItem.IsCollected = rareFirstObtainedDates.Count > 0;
            rareCollectionItem.FirstObtainedUtc = rareFirstObtainedDates.Count > 0
                ? rareFirstObtainedDates.Min()
                : null;

            items.Add(rareCollectionItem);
        }

        items = items
            // Keep a case collection readable at a glance. Collection state should never move
            // Gold items ahead of the normal CS rarity progression.
            .OrderBy(item => GetCollectionRarityOrder(item.RarityKey))
            .ThenBy(item => item.Name)
            .ToList();

        return new CaseOpeningCollectionObj
        {
            CaseKey = caseData.CaseKey,
            CaseName = caseData.Name,
            TotalItemCount = items.Count,
            CollectedItemCount = items.Count(item => item.IsCollected),
            Items = items
        };
    }

    public async Task<CaseOpeningBotProgressObj> GetCaseOpeningBotProgress(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        List<CaseOpeningBotServerDbModel> servers = await _data.GetCaseOpeningBotServers(userId, cancellationToken);
        List<CaseOpeningBotDbModel> bots = await _data.GetCaseOpeningBots(userId, cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);

        return CreateBotProgress(progress, servers, bots, settings);
    }

    public async Task<CaseOpeningBotProgressObj> PurchaseCaseOpeningBotServer(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningBotProgressObj current = await GetCaseOpeningBotProgress(userId, cancellationToken);
        if (current.ActiveBalanceMinor < current.ActiveNextServerCost)
        {
            throw new InvalidOperationException($"You need {current.NextServerCost} Stars to purchase the next bot server.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        long gbpCost = settings.BotServerBaseCostGbpPence + (current.Servers.Count * settings.BotServerCostIncrementGbpPence);
        await _data.PurchaseCaseOpeningBotServer(userId, Guid.NewGuid(), current.NextServerCost, gbpCost, cancellationToken);
        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: current.NextServerCost, upgradesPurchased: 1, cancellationToken: cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        return await GetCaseOpeningBotProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningBotProgressObj> PurchaseCaseOpeningBot(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningBotProgressObj current = await GetCaseOpeningBotProgress(userId, cancellationToken);
        CaseOpeningBotServerObj? server = current.Servers.FirstOrDefault(item => item.Bots.Count < BotServerCapacity);
        if (server is null)
        {
            throw new InvalidOperationException("Purchase a bot server before adding another bot.");
        }

        if (current.ActiveBalanceMinor < current.ActiveNextBotCost)
        {
            throw new InvalidOperationException($"You need {current.NextBotCost} Stars to purchase the next bot.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        int ownedBots = current.Servers.Sum(item => item.Bots.Count);
        long gbpCost = (long)Math.Ceiling((double)settings.BotBaseCostGbpPence * Math.Pow((double)settings.BotCostGrowthRate, ownedBots));
        await _data.PurchaseCaseOpeningBot(userId, server.ServerId, Guid.NewGuid(), current.NextBotCost, gbpCost, cancellationToken);
        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: current.NextBotCost, upgradesPurchased: 1, cancellationToken: cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        return await GetCaseOpeningBotProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningBotProgressObj> UpgradeCaseOpeningBotServer(Guid userId, Guid serverId, CancellationToken cancellationToken = default)
    {
        CaseOpeningBotProgressObj current = await GetCaseOpeningBotProgress(userId, cancellationToken);
        CaseOpeningBotServerObj server = current.Servers.FirstOrDefault(item => item.ServerId == serverId)
            ?? throw new InvalidOperationException("The selected bot server could not be found.");
        CaseOpeningBotDbModel bot = server.Bots.FirstOrDefault(item => !item.MaximumSpeedReached)
            ?? throw new InvalidOperationException("Every bot in this rack is already at level 5.");
        return await UpgradeCaseOpeningBot(userId, bot.BotId, cancellationToken);
    }

    public async Task<CaseOpeningBotProgressObj> UpgradeCaseOpeningBot(Guid userId, Guid botId, CancellationToken cancellationToken = default)
    {
        CaseOpeningBotProgressObj current = await GetCaseOpeningBotProgress(userId, cancellationToken);
        CaseOpeningBotDbModel bot = current.Servers.SelectMany(item => item.Bots).FirstOrDefault(item => item.BotId == botId)
            ?? throw new InvalidOperationException("The selected opening bot could not be found.");
        if (bot.MaximumSpeedReached) throw new InvalidOperationException("This opening bot is already at level 5.");
        if (current.ActiveBalanceMinor < bot.ActiveNextSpeedUpgradeCost)
        {
            throw new InvalidOperationException($"You need {bot.NextSpeedUpgradeCost} Stars for this bot upgrade.");
        }

        await _data.UpgradeCaseOpeningBot(userId, botId, bot.NextSpeedUpgradeCost, bot.ActiveNextSpeedUpgradeCost, MaximumIndividualBotSpeedLevel, cancellationToken);
        await RecordPlayerActivity(userId, starsSpent: bot.NextSpeedUpgradeCost, upgradesPurchased: 1, cancellationToken: cancellationToken);
        return await GetCaseOpeningBotProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningBotProgressObj> SetCaseOpeningBotServerEnabled(Guid userId, Guid serverId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        await _data.SetCaseOpeningBotServerEnabled(userId, serverId, isEnabled, cancellationToken);
        return await GetCaseOpeningBotProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningResultObj> OpenCaseWithBot(
        Guid userId,
        Guid botId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unlock this case before assigning it to a bot.");
        }

        int ownedQuantity = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .FirstOrDefault(item => item.CaseKey.Equals(caseKey, StringComparison.OrdinalIgnoreCase))?.Quantity ?? 0;
        if (ownedQuantity < 1)
        {
            throw new InvalidOperationException("This bot needs an owned case to open. Buy more from the Shop when it is available.");
        }

        if (!await _data.ClaimCaseOpeningBotCycle(userId, botId, cancellationToken))
        {
            throw new InvalidOperationException("This bot is still cooling down. It can open another case shortly.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        Dictionary<string, int> xpByRarity = await GetXpByRarityByKey(cancellationToken);
        HashSet<string> forcedRarityKeys = await GetDevForcedRarityKeys(userId, cancellationToken);
        CaseOpeningResultObj result = await OpenCase(userId, caseKey, cancellationToken, xpByRarity, settings.XpPerCaseOpen, forcedRarityKeys: forcedRarityKeys);
        await NotifyLiveWinnersChanged(cancellationToken);
        return result;
    }

    public async Task<CaseOpeningBotCycleResultObj> RunCaseOpeningBotCycle(Guid userId, string caseKey, CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        CaseOpeningGameSettingsObj economySettings = await _data.GetGameSettings(cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> configuredCases = await GetCaseSettingsByKey(cancellationToken);
        if (configuredCases.TryGetValue(caseKey, out CaseOpeningCaseSettingsObj? configuredCase)
            && (IsGbp(economySettings) ? configuredCase.PurchaseCostGbpPence : configuredCase.PurchaseCostStars) == 0)
        {
            return new CaseOpeningBotCycleResultObj { ShouldStop = true, StopReason = "free-case", Message = "Bots cannot open free cases. Choose a paid case for this rack." };
        }
        int ownedQuantity = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .FirstOrDefault(item => item.CaseKey.Equals(caseKey, StringComparison.OrdinalIgnoreCase))?.Quantity ?? 0;
        if (ownedQuantity < 1)
        {
            return new CaseOpeningBotCycleResultObj { ShouldStop = true, StopReason = "out-of-stock", Message = "Bots paused because the assigned case is out of stock." };
        }

        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            return new CaseOpeningBotCycleResultObj { ShouldStop = true, StopReason = "case-locked", Message = "Bots paused because the assigned case is no longer unlocked." };
        }

        CaseOpeningBotProgressObj progress = await GetCaseOpeningBotProgress(userId, cancellationToken);
        List<CaseOpeningBotDbModel> bots = progress.Servers.Where(server => server.IsEnabled)
            .SelectMany(server => server.Bots).Take(ownedQuantity).ToList();
        if (bots.Count == 0)
        {
            return new CaseOpeningBotCycleResultObj { ShouldStop = true, StopReason = "no-active-bots", Message = "No bot servers are currently online." };
        }

        CaseOpeningBotCycleResultObj cycle = new();
        CaseOpeningGameSettingsObj settings = economySettings;
        Dictionary<string, int> xpByRarity = await GetXpByRarityByKey(cancellationToken);
        CaseOpeningInventoryUpgradeDbModel autoSellSettings = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> caseSaleSettings = await GetCaseSettingsByKey(cancellationToken);
        HashSet<string> forcedRarityKeys = await GetDevForcedRarityKeys(userId, cancellationToken);
        foreach (CaseOpeningBotDbModel bot in bots)
        {
            try
            {
                if (!await _data.ClaimCaseOpeningBotCycle(userId, bot.BotId, cancellationToken)) continue;
                cycle.Results.Add(await OpenCase(userId, caseKey, cancellationToken, xpByRarity, settings.XpPerCaseOpen, autoSellSettings, caseSaleSettings, forcedRarityKeys: forcedRarityKeys));
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("owned case", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("do not own", StringComparison.OrdinalIgnoreCase))
            {
                cycle.ShouldStop = true;
                cycle.StopReason = "out-of-stock";
                cycle.Message = "Bots paused because the assigned case is out of stock.";
                break;
            }
        }
        if (cycle.Results.Count > 0) await NotifyLiveWinnersChanged(cancellationToken);
        return cycle;
    }

    public async Task<CaseOpeningProgressObj> GetCaseOpeningProgress(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        return await BuildProgress(
            await _data.GetCaseOpeningProgress(userId, cancellationToken),
            settings,
            await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken),
            cancellationToken);
    }

    public async Task<CaseOpeningInventoryCapacityObj> GetCaseOpeningInventoryCapacity(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return (await _data.GetCaseOpeningInventoryCapacity(userId, cancellationToken))
            .Adapt<CaseOpeningInventoryCapacityObj>();
    }

    public async Task<CaseOpeningPlayerStatsObj> GetCaseOpeningPlayerStats(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return (await _data.GetCaseOpeningPlayerStats(userId, cancellationToken))
            .Adapt<CaseOpeningPlayerStatsObj>();
    }

    public async Task<CaseOpeningAchievementSummaryObj> GetCaseOpeningAchievements(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EvaluateAchievements(userId, cancellationToken);

        CaseOpeningPlayerStatsDbModel stats = await _data.GetCaseOpeningPlayerStats(userId, cancellationToken);
        CaseOpeningGameSettingsObj economySettings = await _data.GetGameSettings(cancellationToken);
        List<CaseOpeningAchievementObj> achievements = (await _data.GetCaseOpeningAchievements(userId, cancellationToken))
            .Select(achievement =>
            {
                CaseOpeningAchievementObj result = achievement.Adapt<CaseOpeningAchievementObj>();
                result.CurrentValue = GetAchievementMetricValue(stats, achievement.MetricKey);
                result.RewardAmountMinor = IsGbp(economySettings) ? achievement.RewardGbpPence : achievement.RewardStars;
                return result;
            })
            .OrderBy(achievement => achievement.SortOrder)
            .ToList();

        return new CaseOpeningAchievementSummaryObj
        {
            Stats = stats.Adapt<CaseOpeningPlayerStatsObj>(),
            UnlockedCount = achievements.Count(achievement => achievement.IsUnlocked),
            TotalCount = achievements.Count,
            EarnedStars = achievements
                .Where(achievement => achievement.IsUnlocked)
                .Sum(achievement => achievement.RewardStars),
            EarnedAmountMinor = achievements.Where(achievement => achievement.IsUnlocked).Sum(achievement => achievement.RewardAmountMinor),
            Achievements = achievements
        };
    }

    /// <summary>
    /// Login activity is recorded separately from authentication. A failure here must not stop a
    /// valid account signing in, so the caller deliberately treats it as best-effort telemetry.
    /// </summary>
    public Task RecordCaseOpeningLogin(Guid userId, CancellationToken cancellationToken = default)
    {
        return RecordLoginAndEvaluateAchievements(userId, cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> UnlockCaseOpeningCase(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        await _referenceData.GetCase(caseKey, cancellationToken);

        Dictionary<string, CaseOpeningCaseSettingsObj> caseSettings = await GetCaseSettingsByKey(cancellationToken);
        if (!caseSettings.TryGetValue(caseKey, out CaseOpeningCaseSettingsObj? settings))
        {
            throw new InvalidOperationException("This case does not have an unlock price configured yet.");
        }

        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This case is already unlocked.");
        }

        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        if (settings.XpRequirement > 0 && CaseOpeningXpLevels.GetLevel(progress.Xp) < settings.XpRequirement)
        {
            throw new InvalidOperationException($"Reach level {settings.XpRequirement} to unlock this case.");
        }

        CaseOpeningGameSettingsObj gameSettings = await _data.GetGameSettings(cancellationToken);
        long unlockCost = IsGbp(gameSettings) ? settings.UnlockCostGbpPence : settings.UnlockCostStars;
        long balance = IsGbp(gameSettings) ? progress.GbpPence : progress.Stars;
        if (balance < unlockCost)
        {
            throw new InvalidOperationException("You do not have enough currency to unlock this case.");
        }

        CaseOpeningProgressDbModel? updated = await _data.UnlockCaseOpeningCase(userId, caseKey, settings.UnlockCostStars, settings.UnlockCostGbpPence, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("The case could not be unlocked because your balance changed. Please try again.");
        }

        unlockedCaseKeys.Add(caseKey);
        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: IsGbp(gameSettings) ? 0 : settings.UnlockCostStars, cancellationToken: cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        return await BuildProgress(
            await _data.GetCaseOpeningProgress(userId, cancellationToken),
            gameSettings,
            unlockedCaseKeys,
            cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> UnlockCaseOpeningUpgrade(
        Guid userId,
        string upgradeKey,
        CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj gameSettings = await _data.GetGameSettings(cancellationToken);
        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);

        // Speed's cost/requirement climb with the level already owned (mirrors the bot speed
        // upgrade's base+increment formula), unlike multi-open which is flat. Skip animation is no
        // longer independently purchasable - it's granted automatically as the final speed level
        // (see sp_case_opening_upgrade_unlock), so there's no "skip-animation" case here anymore.
        (string Key, int Cost, int XpRequirement, Func<CaseOpeningProgressDbModel, bool> IsUnlocked) upgrade = upgradeKey switch
        {
            "multi-open" => ("multi-open", gameSettings.MultiOpenCostStars, gameSettings.MultiOpenXpRequirement, p => p.MultiOpenLevel >= gameSettings.MaximumMultiOpenLevel),
            "open-speed" => (
                "open-speed",
                gameSettings.OpenSpeedUpgradeBaseCostStars + (progress.OpenSpeedLevel * gameSettings.OpenSpeedUpgradeCostIncrementStars),
                gameSettings.OpenSpeedUpgradeXpRequirement + progress.OpenSpeedLevel,
                p => p.OpenSpeedLevel >= gameSettings.MaximumOpenSpeedLevel),
            _ => throw new InvalidOperationException("That case-opening upgrade is not available.")
        };

        if (upgrade.IsUnlocked(progress))
        {
            throw new InvalidOperationException("That case-opening upgrade is already unlocked.");
        }

        if (upgrade.XpRequirement > 0 && CaseOpeningXpLevels.GetLevel(progress.Xp) < upgrade.XpRequirement)
        {
            throw new InvalidOperationException($"Reach level {upgrade.XpRequirement} to unlock this upgrade.");
        }

        long upgradeCostGbpPence = upgrade.Key == "multi-open"
            ? gameSettings.MultiOpenCostGbpPence
            : gameSettings.OpenSpeedUpgradeBaseCostGbpPence + (progress.OpenSpeedLevel * gameSettings.OpenSpeedUpgradeCostIncrementGbpPence);
        long activeUpgradeCost = IsGbp(gameSettings) ? upgradeCostGbpPence : upgrade.Cost;
        if ((IsGbp(gameSettings) ? progress.GbpPence : progress.Stars) < activeUpgradeCost)
        {
            throw new InvalidOperationException("You do not have enough currency to unlock this upgrade.");
        }

        CaseOpeningProgressDbModel? updated = await _data.UnlockCaseOpeningUpgrade(
            userId,
            upgrade.Key,
            upgrade.Cost,
            upgradeCostGbpPence,
            gameSettings.MaximumMultiOpenLevel,
            gameSettings.MaximumOpenSpeedLevel,
            cancellationToken);

        if (updated is null)
        {
            throw new InvalidOperationException("The upgrade could not be unlocked because your balance changed. Please try again.");
        }

        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: IsGbp(gameSettings) ? 0 : upgrade.Cost, upgradesPurchased: 1, cancellationToken: cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        return await BuildProgress(
            await _data.GetCaseOpeningProgress(userId, cancellationToken),
            gameSettings,
            null,
            cancellationToken);
    }

    public async Task<CaseOpeningSellResultObj> SellCaseOpeningInventory(
        Guid userId,
        List<Guid> openingIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> selectedIds = openingIds.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one inventory item to sell.");
        }

        CaseOpeningInventoryUpgradeDbModel inventoryUpgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        if (selectedIds.Count > inventoryUpgrades.BulkSellLimit)
        {
            throw new InvalidOperationException($"Your current bulk-sale limit is {inventoryUpgrades.BulkSellLimit} items.");
        }

        List<CaseOpeningHistoryDbModel> inventory = await _data.GetCaseOpeningHistory(userId, cancellationToken);
        List<CaseOpeningHistoryDbModel> selectedItems = inventory
            .Where(item => selectedIds.Contains(item.OpeningId))
            .ToList();

        if (selectedItems.Count != selectedIds.Count)
        {
            throw new InvalidOperationException("One or more selected inventory items could not be sold. Refresh your inventory and try again.");
        }

        if (selectedItems.Any(item => item.IsLocked))
        {
            throw new InvalidOperationException("Unlock protected items before selling them.");
        }

        CaseOpeningGameSettingsObj gameSettings = await _data.GetGameSettings(cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> caseSettings = await GetCaseSettingsByKey(cancellationToken);
        List<CaseOpeningSaleAward> awards = selectedItems.Select(item =>
        {
            (int unlockCost, _, _) = GetCaseSettings(caseSettings, item.CaseKey);
            int fallbackStars = GetSaleValue(item.RarityKey) * GetCaseSaleMultiplier(unlockCost);
            return CaseOpeningBalancePolicy.CalculateSaleAward(item.EstimatedPrice, gameSettings.SkinSaleRateBasisPoints, fallbackStars);
        }).ToList();
        int starsAwarded = awards.Sum(item => item.Stars);
        long gbpPenceAwarded = awards.Sum(item => item.GbpPence);
        CaseOpeningSellResultDbModel? result = await _data.SellCaseOpeningInventory(
            userId,
            selectedIds,
            starsAwarded,
            gbpPenceAwarded,
            cancellationToken);
        if (result is null || result.SoldItemCount != selectedIds.Count)
        {
            throw new InvalidOperationException("One or more selected inventory items could not be sold. Refresh your inventory and try again.");
        }

        await RecordPlayerActivity(userId, saleStarsEarned: IsGbp(gameSettings) ? 0 : result.StarsAwarded, cancellationToken: cancellationToken);

        return result.Adapt<CaseOpeningSellResultObj>();
    }

    public async Task<CaseOpeningProgressObj> ClaimCaseOpeningDailyDrop(Guid userId, List<string> rewardKeys, CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        CaseOpeningDailyDropDbModel daily = await _data.GetCaseOpeningDailyDrop(userId, cancellationToken);
        List<CaseOpeningDailyDropRewardObj> offer = ParseDailyDropOffer(daily.OfferJson);
        List<CaseOpeningDailyDropRewardObj> selected = offer.Where(reward => rewardKeys.Contains(reward.RewardKey, StringComparer.OrdinalIgnoreCase)).ToList();
        if (selected.Count != 2 || rewardKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
            throw new InvalidOperationException("Choose exactly two Daily Drop rewards.");
        await _data.ClaimCaseOpeningDailyDrop(userId, rewardKeys, settings.EconomyMode, cancellationToken);
        foreach (CaseOpeningDailyDropRewardObj skin in selected.Where(reward => reward.Kind == "skin" && reward.Item is not null))
        {
            CaseOpeningHistoryDbModel history = skin.Item!.Adapt<CaseOpeningHistoryDbModel>();
            history.OpeningId = Guid.NewGuid(); history.UserId = userId; history.CaseKey = skin.CaseKey ?? string.Empty; history.OpenedUtc = DateTime.UtcNow;
            await _data.SaveCaseOpening(userId, history, cancellationToken);
        }
        if (selected.Any(reward => reward.Kind == "skin" && reward.Item is not null)) await NotifyLiveWinnersChanged(cancellationToken);
        return await GetCaseOpeningProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> UnlockCaseOpeningDailyDropUpgrade(Guid userId, string upgradeKey, CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        Dictionary<string, int> levels = (await _data.GetCaseOpeningDailyDropUpgrades(userId, cancellationToken)).ToDictionary(item => item.UpgradeKey, item => item.Level, StringComparer.OrdinalIgnoreCase);
        (int max, int stars, long pence) = DailyDropUpgradeCost(upgradeKey, levels.GetValueOrDefault(upgradeKey));
        if (levels.GetValueOrDefault(upgradeKey) >= max) throw new InvalidOperationException("This Daily Drop upgrade is already maxed.");
        await _data.UnlockCaseOpeningDailyDropUpgrade(userId, upgradeKey, stars, pence, settings.EconomyMode, cancellationToken);
        return await GetCaseOpeningProgress(userId, cancellationToken);
    }
    public Task<int> GetDailyDropRequiredXp(CancellationToken cancellationToken = default) => _data.GetCaseOpeningDailyDropRequiredXp(cancellationToken);
    public async Task SetDailyDropRequiredXp(int requiredXp, CancellationToken cancellationToken = default) { if (requiredXp is < 1 or > 10000) throw new InvalidOperationException("Daily Drop XP must be between 1 and 10,000."); await _data.SetCaseOpeningDailyDropRequiredXp(requiredXp, cancellationToken); }
    public async Task<CaseOpeningProgressObj> ResetDailyDrop(Guid userId, CancellationToken cancellationToken = default) { await _data.ResetCaseOpeningDailyDrop(userId, cancellationToken); return await GetCaseOpeningProgress(userId, cancellationToken); }

    public async Task<CaseOpeningInventoryLockObj> SetCaseOpeningInventoryLock(
        Guid userId,
        Guid openingId,
        bool isLocked,
        CancellationToken cancellationToken = default)
    {
        CaseOpeningInventoryLockObj? savedState = await _data.SetCaseOpeningInventoryLock(userId, openingId, isLocked, cancellationToken);
        if (savedState is null)
        {
            throw new InvalidOperationException("That inventory item could not be found. Refresh your inventory and try again.");
        }

        return savedState;
    }

    public async Task<CaseOpeningInventoryUpgradeObj> GetCaseOpeningInventoryUpgrades(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningInventoryUpgradeObj result = (await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken))
            .Adapt<CaseOpeningInventoryUpgradeObj>();
        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        result.Stars = progress.Stars;
        result.GbpPence = progress.GbpPence;
        result.EconomyMode = settings.EconomyMode;
        result.ActiveBalanceMinor = IsGbp(settings) ? progress.GbpPence : progress.Stars;
        result.AvailableUpgrades = await _data.GetCaseOpeningUpgradeDefinitions(userId, cancellationToken);
        result.AvailableUpgrades.ForEach(item => item.Cost = IsGbp(settings) ? item.CostGbpPence : item.CostStars);
        result.MaximumTradeUpRecipeSlots = MaximumTradeUpRecipeSlots;
        result.TradeUpRecipeSlotUpgradeCostStars = result.TradeUpRecipesUnlocked && result.TradeUpRecipeSlots < MaximumTradeUpRecipeSlots
            ? settings.TradeUpSlotUpgradeBaseCostStars + (result.TradeUpRecipeSlots * settings.TradeUpSlotUpgradeCostIncrementStars)
            : 0;
        result.TradeUpRecipeSlotUpgradeCost = result.TradeUpRecipesUnlocked && result.TradeUpRecipeSlots < MaximumTradeUpRecipeSlots
            ? (IsGbp(settings)
                ? settings.TradeUpSlotUpgradeBaseCostGbpPence + (result.TradeUpRecipeSlots * settings.TradeUpSlotUpgradeCostIncrementGbpPence)
                : result.TradeUpRecipeSlotUpgradeCostStars)
            : 0;
        return result;
    }

    public async Task<CaseOpeningInventoryUpgradeObj> UnlockCaseOpeningInventoryUpgrade(Guid userId, string upgradeKey, CancellationToken cancellationToken = default)
    {
        List<CaseOpeningUpgradeDefinitionObj> definitions = await _data.GetCaseOpeningUpgradeDefinitions(userId, cancellationToken);
        CaseOpeningUpgradeDefinitionObj? definition = definitions
            .FirstOrDefault(item => item.UpgradeKey.Equals(upgradeKey, StringComparison.OrdinalIgnoreCase));
        if (definition is null || definition.IsUnlocked)
        {
            throw new InvalidOperationException(definition is null ? "That inventory upgrade is not available." : "That inventory upgrade is already unlocked.");
        }

        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        if (CaseOpeningXpLevels.GetLevel(progress.Xp) < definition.RequiredLevel)
        {
            throw new InvalidOperationException($"Reach level {definition.RequiredLevel} to unlock {definition.Name}.");
        }
        if ((IsGbp(settings) ? progress.GbpPence : progress.Stars) < (IsGbp(settings) ? definition.CostGbpPence : definition.CostStars))
        {
            throw new InvalidOperationException($"You do not have enough currency to unlock {definition.Name}.");
        }

        // Capacity tiers are cumulative. Requiring the previous tier keeps the upgrade path
        // understandable and prevents a high-balance account from skipping the progression.
        string? requiredUpgradeKey = definition.UpgradeKey.ToLowerInvariant() switch
        {
            "inventory-slots-500" => "inventory-slots-250",
            "inventory-slots-1000" => "inventory-slots-500",
            "bulk-sell-300" => "bulk-sell-200",
            "bulk-sell-400" => "bulk-sell-300",
            "bulk-sell-500" => "bulk-sell-400",
            "auto-sell-classified" => "auto-sell-covert",
            "auto-sell-restricted" => "auto-sell-classified",
            "auto-sell-mil-spec" => "auto-sell-restricted",
            "auto-buy-slots-10" => "auto-buy-slots-5",
            "trade-up-holding-5" => "trade-up-unlock",
            "trade-up-holding-10" => "trade-up-holding-5",
            "trade-up-holding-20" => "trade-up-holding-10",
            _ => null
        };
        if (requiredUpgradeKey is not null && !definitions.Any(item =>
                item.UpgradeKey.Equals(requiredUpgradeKey, StringComparison.OrdinalIgnoreCase) && item.IsUnlocked))
        {
            CaseOpeningUpgradeDefinitionObj? requiredUpgrade = definitions.FirstOrDefault(item =>
                item.UpgradeKey.Equals(requiredUpgradeKey, StringComparison.OrdinalIgnoreCase));
            throw new InvalidOperationException($"Unlock {requiredUpgrade?.Name ?? "the previous capacity tier"} first.");
        }

        await _data.UnlockCaseOpeningInventoryUpgrade(userId, definition.UpgradeKey, definition.CostStars, definition.CostGbpPence, cancellationToken);
        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: IsGbp(settings) ? 0 : definition.CostStars, upgradesPurchased: 1, cancellationToken: cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        return await GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
    }

    public async Task<CaseOpeningInventoryUpgradeObj> SetCaseOpeningAutoSellPreference(Guid userId, string rarityKey, bool enabled, bool? preserveStatTrak, CancellationToken cancellationToken = default)
    {
        string normalized = rarityKey.Trim().ToLowerInvariant();
        if (normalized is not ("covert" or "classified" or "restricted" or "mil-spec"))
        {
            throw new InvalidOperationException("That rarity cannot be configured for automatic selling.");
        }
        CaseOpeningInventoryUpgradeDbModel current = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        bool unlocked = normalized switch
        {
            "covert" => current.AutoSellCovertUnlocked,
            "classified" => current.AutoSellClassifiedUnlocked,
            "restricted" => current.AutoSellRestrictedUnlocked,
            _ => current.AutoSellMilSpecUnlocked
        };
        if (enabled && !unlocked)
        {
            throw new InvalidOperationException("Unlock this automatic-sale tier before enabling it.");
        }
        await _data.SetCaseOpeningAutoSellPreference(userId, normalized, enabled, preserveStatTrak ?? current.PreserveStatTrak, cancellationToken);
        return await GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
    }

    public Task<CaseOpeningAutoBuySummaryObj> GetCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default)
    {
        return BuildAutoBuySummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningAutoBuySummaryObj> SetCaseOpeningAutoBuyRule(
        Guid userId,
        string caseKey,
        CaseOpeningAutoBuyRuleRequestObj request,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unlock this case before adding an auto-buy rule for it.");
        }

        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        if (!upgrades.AutoBuyUnlocked)
        {
            throw new InvalidOperationException("Unlock Auto-buy before configuring a rule.");
        }

        if (request.ThresholdQuantity < 0 || request.PurchaseQuantity < 1)
        {
            throw new InvalidOperationException("Threshold cannot be negative and purchase quantity must be at least 1.");
        }

        await _data.SetCaseOpeningAutoBuyRule(
            userId, caseKey, request.ThresholdQuantity, request.PurchaseQuantity, request.IsEnabled, upgrades.AutoBuyRuleSlots, cancellationToken);
        return await BuildAutoBuySummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningAutoBuySummaryObj> DeleteCaseOpeningAutoBuyRule(Guid userId, string caseKey, CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        await _data.DeleteCaseOpeningAutoBuyRule(userId, caseKey, cancellationToken);
        return await BuildAutoBuySummary(userId, cancellationToken);
    }

    private async Task<CaseOpeningAutoBuySummaryObj> BuildAutoBuySummary(Guid userId, CancellationToken cancellationToken)
    {
        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        List<CaseOpeningAutoBuyRuleDbModel> rules = await _data.GetCaseOpeningAutoBuyRules(userId, cancellationToken);
        List<CaseOpeningOwnedCaseDbModel> owned = await _data.GetCaseOpeningOwnedCases(userId, cancellationToken);
        Dictionary<string, int> ownedByKey = owned.ToDictionary(item => item.CaseKey, item => item.Quantity, StringComparer.OrdinalIgnoreCase);
        List<CaseOpeningCaseObj> catalogue = await _referenceData.GetCuratedCases(cancellationToken);
        Dictionary<string, CaseOpeningCaseObj> catalogueByKey = catalogue.ToDictionary(item => item.CaseKey, StringComparer.OrdinalIgnoreCase);

        return new CaseOpeningAutoBuySummaryObj
        {
            Unlocked = upgrades.AutoBuyUnlocked,
            RuleSlots = upgrades.AutoBuyRuleSlots,
            UsedRuleSlots = rules.Count(item => item.IsEnabled),
            Rules = rules.Select(rule =>
            {
                catalogueByKey.TryGetValue(rule.CaseKey, out CaseOpeningCaseObj? caseInfo);
                return new CaseOpeningAutoBuyRuleObj
                {
                    CaseKey = rule.CaseKey,
                    ThresholdQuantity = rule.ThresholdQuantity,
                    PurchaseQuantity = rule.PurchaseQuantity,
                    IsEnabled = rule.IsEnabled,
                    CreatedUtc = rule.CreatedUtc,
                    UpdatedUtc = rule.UpdatedUtc,
                    CaseName = caseInfo?.Name ?? rule.CaseKey,
                    ImageUrl = caseInfo?.ImageUrl ?? string.Empty,
                    OwnedQuantity = ownedByKey.TryGetValue(rule.CaseKey, out int quantity) ? quantity : 0
                };
            }).ToList()
        };
    }

    public async Task<CaseOpeningCasePurchaseResultObj> PurchaseCaseOpeningCases(
        Guid userId,
        string caseKey,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        await _referenceData.GetCase(caseKey, cancellationToken);
        // One transaction can add a useful batch to the player's stock without turning the
        // Shop into hundreds of small requests. The limit is still enforced here because the
        // browser quantity control is only a convenience and cannot be trusted.
        if (quantity < 1 || quantity > 500)
        {
            throw new InvalidOperationException("Buy between 1 and 500 cases at a time.");
        }

        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unlock this case before buying copies from the Shop.");
        }

        Dictionary<string, CaseOpeningCaseSettingsObj> settings = await GetCaseSettingsByKey(cancellationToken);
        if (!settings.TryGetValue(caseKey, out CaseOpeningCaseSettingsObj? caseSettings) || caseSettings.PurchaseCostStars < 0)
        {
            throw new InvalidOperationException("This case does not have a purchase price configured yet.");
        }

        CaseOpeningInventoryCapacityDbModel capacity = await _data.GetCaseOpeningInventoryCapacity(userId, cancellationToken);
        if (quantity > capacity.AvailableSlots)
        {
            throw new InvalidOperationException(capacity.AvailableSlots == 0
                ? "Your inventory is full. Sell skins or unlock more storage before buying cases."
                : $"Only {capacity.AvailableSlots:N0} inventory slots are available. Reduce the quantity or unlock more storage.");
        }

        CaseOpeningCasePurchaseResultObj? result = await _data.PurchaseCaseOpeningCases(userId, caseKey, quantity, caseSettings.PurchaseCostStars, caseSettings.PurchaseCostGbpPence, cancellationToken);
        if (result is null) throw new InvalidOperationException("The case purchase could not be completed. Please try again.");
        await RecordPlayerActivity(userId, casesPurchased: result.PurchasedQuantity, casePurchaseStarsSpent: result.StarsSpent, starsSpent: result.StarsSpent, cancellationToken: cancellationToken);
        return result;
    }

    // Polled by the client every ~20s (mirroring how bots are driven), plus opportunistically right
    // after any case open - the two events that can drop an owned quantity below a rule's threshold.
    // Reuses PurchaseCaseOpeningCases outright so tiered pricing/capacity/unlock checks always match
    // what a manual purchase would enforce - nothing here duplicates that logic. A rule that can't
    // currently afford or fit its purchase (insufficient Stars, full inventory, etc.) is skipped
    // quietly rather than surfaced as an error, since this runs unattended on a timer.
    public async Task<List<CaseOpeningCasePurchaseResultObj>> EvaluateCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        if (!upgrades.AutoBuyUnlocked)
        {
            return [];
        }

        List<CaseOpeningAutoBuyRuleDbModel> rules = await _data.GetCaseOpeningAutoBuyRules(userId, cancellationToken);
        List<CaseOpeningAutoBuyRuleDbModel> enabledRules = rules.Where(rule => rule.IsEnabled).ToList();
        if (enabledRules.Count == 0)
        {
            return [];
        }

        List<CaseOpeningOwnedCaseDbModel> owned = await _data.GetCaseOpeningOwnedCases(userId, cancellationToken);
        Dictionary<string, int> ownedByKey = owned.ToDictionary(item => item.CaseKey, item => item.Quantity, StringComparer.OrdinalIgnoreCase);

        List<CaseOpeningCasePurchaseResultObj> purchases = [];
        foreach (CaseOpeningAutoBuyRuleDbModel rule in enabledRules)
        {
            int ownedQuantity = ownedByKey.TryGetValue(rule.CaseKey, out int quantity) ? quantity : 0;
            if (ownedQuantity >= rule.ThresholdQuantity)
            {
                continue;
            }

            try
            {
                purchases.Add(await PurchaseCaseOpeningCases(userId, rule.CaseKey, rule.PurchaseQuantity, cancellationToken));
            }
            catch (InvalidOperationException)
            {
                // Not enough Stars, no inventory room, case no longer unlocked, etc. - try again
                // next poll rather than failing the whole evaluation for every other rule.
            }
        }

        return purchases;
    }

    public async Task<CaseOpeningStoragePurchaseResultObj> PurchaseCaseOpeningStorageContainer(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        CaseOpeningInventoryCapacityDbModel capacity = await _data.GetCaseOpeningInventoryCapacity(userId, cancellationToken);
        if (capacity.StorageContainerCount >= settings.MaximumStorageContainers)
        {
            throw new InvalidOperationException("You already own the maximum number of storage containers.");
        }

        int cost = settings.StorageContainerBaseCostStars + (capacity.StorageContainerCount * settings.StorageContainerCostIncrementStars);
        long gbpCost = settings.StorageContainerBaseCostGbpPence + (capacity.StorageContainerCount * settings.StorageContainerCostIncrementGbpPence);
        CaseOpeningStoragePurchaseResultObj? result = await _data.PurchaseCaseOpeningStorageContainer(
            userId, Guid.NewGuid(), cost, gbpCost, settings.StorageContainerSlots, settings.MaximumStorageContainers, cancellationToken);
        if (result is null) throw new InvalidOperationException("The storage container could not be purchased. Please try again.");
        await RecordPlayerActivity(userId, starsSpent: result.StarsSpent, upgradesPurchased: 1, cancellationToken: cancellationToken);
        return result;
    }

    /// <summary>
    /// Converts ten owned skins into one next-rarity output. Collection chance is determined by
    /// how many of the ten inputs came from each case, matching the in-game contract principle.
    /// </summary>
    public async Task<CaseOpeningTradeUpResultObj> CreateCaseOpeningTradeUp(
        Guid userId,
        List<Guid> openingIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> selectedIds = openingIds.Distinct().ToList();
        if (selectedIds.Count != 10)
        {
            throw new InvalidOperationException("Select exactly 10 eligible skins for a Trade Up Contract.");
        }

        return await ExecuteTradeUpCore(userId, selectedIds, recipe: null, cancellationToken);
    }

    /// <summary>
    /// Shared by manual Trade Up Contracts and auto trade-up recipes so both run through the exact
    /// same random collection/item/float roll - an auto-fired contract never gets special odds.
    /// When <paramref name="recipe"/> is set, the output's match against the recipe's target is
    /// computed here and persisted atomically with the output row, so a fresh output is held
    /// immediately and never briefly visible in normal inventory.
    /// </summary>
    private async Task<CaseOpeningTradeUpResultObj> ExecuteTradeUpCore(
        Guid userId,
        List<Guid> selectedIds,
        CaseOpeningTradeUpRecipeDbModel? recipe,
        CancellationToken cancellationToken)
    {
        List<CaseOpeningHistoryDbModel> inventory = await _data.GetCaseOpeningHistory(userId, cancellationToken);
        List<CaseOpeningHistoryDbModel> inputs = inventory
            .Where(item => selectedIds.Contains(item.OpeningId))
            .ToList();

        if (inputs.Count != selectedIds.Count)
        {
            throw new InvalidOperationException("One or more selected skins are no longer in your inventory. Refresh and try again.");
        }

        string inputRarityKey = inputs[0].RarityKey;
        if (!TradeUpRarityLadder.TryGetValue(inputRarityKey, out string? outputRarityKey))
        {
            throw new InvalidOperationException("Only Mil-Spec, Restricted and Classified weapon skins can be used in a Trade Up Contract.");
        }

        if (inputs.Any(item => item.IsLocked))
        {
            throw new InvalidOperationException("Unlock protected skins before using them in a Trade Up Contract.");
        }

        if (inputs.Any(item => item.IsRareSpecial || item.RarityKey != inputRarityKey || item.FloatValue is null))
        {
            throw new InvalidOperationException("All 10 contract skins must be standard weapon skins with the same rarity.");
        }

        bool isStatTrakContract = inputs[0].IsStatTrak;
        if (inputs.Any(item => item.IsStatTrak != isStatTrakContract))
        {
            throw new InvalidOperationException("A Trade Up Contract must use either 10 StatTrak™ skins or 10 standard skins.");
        }

        Dictionary<string, CaseOpeningCaseObj> casesByKey = (await _referenceData.GetCuratedCases(cancellationToken))
            .ToDictionary(item => item.CaseKey, StringComparer.OrdinalIgnoreCase);
        List<IGrouping<string, CaseOpeningHistoryDbModel>> groups = inputs
            .GroupBy(item => item.CaseKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (IGrouping<string, CaseOpeningHistoryDbModel> group in groups)
        {
            if (!casesByKey.TryGetValue(group.Key, out CaseOpeningCaseObj? sourceCase)
                || sourceCase.Type is "Sticker Capsule" or "Souvenir Package"
                || !sourceCase.Items.Any(item => item.RarityKey == outputRarityKey
                    && !item.IsRareSpecial
                    && (!isStatTrakContract || item.SupportsStatTrak)))
            {
                throw new InvalidOperationException("One of the selected collections has no valid next-rarity contract output.");
            }
        }

        // Every input has one equal share of the resulting collection chance. A 5/5 split is
        // therefore 50/50, while ten different source collections each receive 10%.
        int collectionRoll = RandomNumberGenerator.GetInt32(inputs.Count);
        int runningTotal = 0;
        IGrouping<string, CaseOpeningHistoryDbModel> selectedCollection = groups
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .First();
        foreach (IGrouping<string, CaseOpeningHistoryDbModel> group in groups.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            runningTotal += group.Count();
            if (collectionRoll < runningTotal)
            {
                selectedCollection = group;
                break;
            }
        }

        CaseOpeningCaseObj outputCase = casesByKey[selectedCollection.Key];
        List<CaseOpeningItemObj> eligibleOutputs = outputCase.Items
            .Where(item => item.RarityKey == outputRarityKey
                && !item.IsRareSpecial
                && (!isStatTrakContract || item.SupportsStatTrak))
            .ToList();
        CaseOpeningItemObj outputItem = Clone(eligibleOutputs[RandomNumberGenerator.GetInt32(eligibleOutputs.Count)]);
        decimal averageInputFloat = decimal.Round(inputs.Average(item => item.FloatValue!.Value), 6);
        await ApplyTradeUpCondition(userId, outputItem, averageInputFloat, isStatTrakContract, cancellationToken);
        outputItem.EstimatedPrice = await _prices.GetEstimatedPrice(outputItem.MarketHashName, cancellationToken);

        CaseOpeningHistoryDbModel output = outputItem.Adapt<CaseOpeningHistoryDbModel>();
        output.OpeningId = Guid.NewGuid();
        output.UserId = userId;
        output.CaseKey = outputCase.CaseKey;
        output.OpenedUtc = DateTime.UtcNow;

        CaseOpeningTradeUpDbModel tradeUp = new()
        {
            TradeUpId = Guid.NewGuid(),
            UserId = userId,
            InputRarityKey = inputRarityKey,
            OutputRarityKey = outputRarityKey,
            OutputOpeningId = output.OpeningId,
            OutputCaseKey = output.CaseKey,
            AverageInputFloat = averageInputFloat
        };
        bool? isMatch = null;
        if (recipe is not null)
        {
            bool wearMatches = recipe.TargetWears.Count == 0 || recipe.TargetWears.Contains(output.Wear);
            isMatch = string.Equals(output.SourceItemId, recipe.TargetSourceItemId, StringComparison.Ordinal) && wearMatches;
        }

        await _data.ExecuteCaseOpeningTradeUp(userId, tradeUp, selectedIds, output, recipe?.RecipeId, isMatch, cancellationToken);
        await RecordPlayerActivity(userId, skinsObtained: 1, tradeUpsCompleted: 1, cancellationToken: cancellationToken);
        await RecordCollectionMilestones(userId, outputCase, cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        await NotifyLiveWinnersChanged(cancellationToken);

        return new CaseOpeningTradeUpResultObj
        {
            TradeUpId = tradeUp.TradeUpId,
            InputRarityName = inputs[0].RarityName,
            OutputRarityName = output.RarityName,
            AverageInputFloat = averageInputFloat,
            Output = output.Adapt<CaseOpeningHistoryObj>(),
            SourceChances = groups
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CaseOpeningTradeUpSourceChanceObj
                {
                    CaseKey = group.Key,
                    CaseName = casesByKey[group.Key].Name,
                    InputCount = group.Count(),
                    Percentage = group.Count() * 10m
                })
                .ToList(),
            RecipeId = recipe?.RecipeId,
            IsMatch = isMatch
        };
    }

    public async Task<CaseOpeningTradeUpRecipeSummaryObj> GetCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default)
    {
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningTradeUpRecipeSummaryObj> CreateCaseOpeningTradeUpRecipe(
        Guid userId,
        CaseOpeningTradeUpRecipeRequestObj request,
        CancellationToken cancellationToken = default)
    {
        string caseKey = string.IsNullOrWhiteSpace(request.CaseKey)
            ? throw new InvalidOperationException("Choose a case for this recipe.")
            : request.CaseKey.Trim();
        string sourceItemId = string.IsNullOrWhiteSpace(request.SourceItemId)
            ? throw new InvalidOperationException("Choose a target skin for this recipe.")
            : request.SourceItemId.Trim();

        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unlock this case before targeting one of its skins.");
        }

        CaseOpeningCaseObj caseData = await _referenceData.GetCase(caseKey, cancellationToken);
        CaseOpeningItemObj? targetItem = caseData.Items.FirstOrDefault(item =>
            string.Equals(item.SourceItemId, sourceItemId, StringComparison.Ordinal) && !item.IsRareSpecial);
        if (targetItem is null)
        {
            throw new InvalidOperationException("That skin is not available in the selected case.");
        }

        string? inputRarityKey = TradeUpRarityLadder
            .FirstOrDefault(pair => string.Equals(pair.Value, targetItem.RarityKey, StringComparison.OrdinalIgnoreCase)).Key;
        if (inputRarityKey is null)
        {
            throw new InvalidOperationException("Only Restricted, Classified or Covert skins have a valid Trade Up input tier.");
        }

        if (request.StatTrak && !targetItem.SupportsStatTrak)
        {
            throw new InvalidOperationException("That skin does not support StatTrak™.");
        }

        List<string> wears = (request.Wears ?? [])
            .Where(wear => KnownWears.Contains(wear))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        if (!upgrades.TradeUpRecipesUnlocked)
        {
            throw new InvalidOperationException("Unlock Auto trade-up in the Upgrades tab first.");
        }

        CaseOpeningTradeUpRecipeDbModel recipe = new()
        {
            RecipeId = Guid.NewGuid(),
            TargetCaseKey = caseData.CaseKey,
            TargetSourceItemId = targetItem.SourceItemId,
            TargetItemName = targetItem.Name,
            TargetMarketHashName = targetItem.MarketHashName,
            TargetImageUrl = targetItem.ImageUrl,
            TargetRarityKey = targetItem.RarityKey,
            TargetRarityName = targetItem.RarityName,
            TargetRarityColor = targetItem.RarityColor,
            TargetInputRarityKey = inputRarityKey,
            TargetStatTrak = request.StatTrak,
            TargetWears = wears
        };

        await _data.CreateCaseOpeningTradeUpRecipe(userId, recipe, settings.TradeUpRecipeCostStars, settings.TradeUpRecipeCostGbpPence, upgrades.TradeUpRecipeSlots, cancellationToken);
        await RecordPlayerActivity(userId, unlocksEarned: 1, starsSpent: IsGbp(settings) ? 0 : settings.TradeUpRecipeCostStars, cancellationToken: cancellationToken);
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningTradeUpRecipeSummaryObj> SetCaseOpeningTradeUpRecipeActive(
        Guid userId,
        Guid recipeId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        await _data.SetCaseOpeningTradeUpRecipeActive(userId, recipeId, isActive, upgrades.TradeUpRecipeSlots, cancellationToken);
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningTradeUpRecipeSummaryObj> DeleteCaseOpeningTradeUpRecipe(
        Guid userId,
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        await _data.DeleteCaseOpeningTradeUpRecipe(userId, recipeId, cancellationToken);
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    public async Task<CaseOpeningTradeUpRecipeSummaryObj> CollectCaseOpeningTradeUpHolding(
        Guid userId,
        Guid holdingId,
        CancellationToken cancellationToken = default)
    {
        await _data.CollectCaseOpeningTradeUpHolding(userId, holdingId, cancellationToken);
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    /// <summary>
    /// Fires at most one contract per active recipe per call, oldest recipe first. Each recipe's
    /// held-item pool is its own - a recipe already at its own HoldingCapacity is skipped even
    /// while every other recipe still has room, since the pool is not shared account-wide.
    /// A recipe with fewer than 10 eligible un-held inputs is silently skipped - it just waits for
    /// the next poll.
    /// </summary>
    public async Task<List<CaseOpeningTradeUpResultObj>> EvaluateCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default)
    {
        List<CaseOpeningTradeUpRecipeObj> recipes = (await _data.GetCaseOpeningTradeUpRecipes(userId, cancellationToken))
            .Where(recipe => recipe.IsActive && recipe.HeldCount < recipe.HoldingCapacity)
            .OrderBy(recipe => recipe.CreatedUtc)
            .ToList();
        if (recipes.Count == 0)
        {
            return [];
        }

        List<CaseOpeningHistoryDbModel> inventory = await _data.GetCaseOpeningHistory(userId, cancellationToken);
        List<CaseOpeningTradeUpResultObj> fired = [];

        foreach (CaseOpeningTradeUpRecipeObj recipe in recipes)
        {
            List<CaseOpeningHistoryDbModel> matchingInputs = inventory
                .Where(item => string.Equals(item.CaseKey, recipe.TargetCaseKey, StringComparison.OrdinalIgnoreCase)
                    && item.RarityKey == recipe.TargetInputRarityKey
                    && !item.IsLocked
                    && !item.IsRareSpecial
                    && item.IsStatTrak == recipe.TargetStatTrak
                    && item.FloatValue is not null)
                .OrderBy(item => item.OpenedUtc)
                .Take(10)
                .ToList();
            if (matchingInputs.Count < 10)
            {
                continue;
            }

            try
            {
                CaseOpeningTradeUpResultObj result = await ExecuteTradeUpCore(
                    userId,
                    matchingInputs.Select(item => item.OpeningId).ToList(),
                    recipe,
                    cancellationToken);
                fired.Add(result);

                HashSet<Guid> consumedIds = matchingInputs.Select(item => item.OpeningId).ToHashSet();
                inventory = inventory.Where(item => !consumedIds.Contains(item.OpeningId)).ToList();
            }
            catch (InvalidOperationException)
            {
                // Inventory changed since the snapshot (another recipe or a manual sale claimed one
                // of these items first) - skip this recipe, it gets another chance next poll.
            }
        }

        return fired;
    }

    /// <summary>
    /// Recipe slots are a repeatable +1 purchase, not a discrete-tier upgrade - mirrors the bot
    /// speed upgrade pattern (UpgradeCaseOpeningBotServer) rather than the auto-buy/inventory-slots
    /// tiered-definition pattern, so buying "the first upgrade" only ever grants exactly one slot.
    /// </summary>
    public async Task<CaseOpeningInventoryUpgradeObj> UpgradeCaseOpeningTradeUpRecipeSlots(Guid userId, CancellationToken cancellationToken = default)
    {
        CaseOpeningInventoryUpgradeDbModel upgrades = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        if (!upgrades.TradeUpRecipesUnlocked)
        {
            throw new InvalidOperationException("Unlock Auto trade-up in the Upgrades tab first.");
        }
        if (upgrades.TradeUpRecipeSlots >= MaximumTradeUpRecipeSlots)
        {
            throw new InvalidOperationException("Auto trade-up recipe slots are already at maximum.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        int costStars = settings.TradeUpSlotUpgradeBaseCostStars + (upgrades.TradeUpRecipeSlots * settings.TradeUpSlotUpgradeCostIncrementStars);
        long costGbpPence = settings.TradeUpSlotUpgradeBaseCostGbpPence + (upgrades.TradeUpRecipeSlots * settings.TradeUpSlotUpgradeCostIncrementGbpPence);
        await _data.UpgradeCaseOpeningTradeUpRecipeSlots(userId, costStars, costGbpPence, MaximumTradeUpRecipeSlots, cancellationToken);
        await RecordPlayerActivity(userId, starsSpent: IsGbp(settings) ? 0 : costStars, upgradesPurchased: 1, cancellationToken: cancellationToken);
        return await GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
    }

    /// <summary>
    /// Holding capacity is a repeatable +1 purchase scoped to one recipe - the pool is not shared
    /// account-wide, so this only ever raises the ceiling for the recipe the player is looking at.
    /// </summary>
    public async Task<CaseOpeningTradeUpRecipeSummaryObj> UpgradeCaseOpeningTradeUpRecipeHolding(
        Guid userId,
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        List<CaseOpeningTradeUpRecipeObj> recipes = await _data.GetCaseOpeningTradeUpRecipes(userId, cancellationToken);
        CaseOpeningTradeUpRecipeObj? recipe = recipes.FirstOrDefault(item => item.RecipeId == recipeId);
        if (recipe is null)
        {
            throw new InvalidOperationException("That recipe could not be found.");
        }
        if (recipe.HoldingCapacity >= MaximumTradeUpRecipeHoldingCapacity)
        {
            throw new InvalidOperationException("This recipe's holding capacity is already at maximum.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        int costStars = settings.TradeUpHoldingUpgradeBaseCostStars + (recipe.HoldingCapacity * settings.TradeUpHoldingUpgradeCostIncrementStars);
        long costGbpPence = settings.TradeUpHoldingUpgradeBaseCostGbpPence + (recipe.HoldingCapacity * settings.TradeUpHoldingUpgradeCostIncrementGbpPence);
        await _data.UpgradeCaseOpeningTradeUpRecipeHolding(userId, recipeId, costStars, costGbpPence, MaximumTradeUpRecipeHoldingCapacity, cancellationToken);
        await RecordPlayerActivity(userId, starsSpent: IsGbp(settings) ? 0 : costStars, upgradesPurchased: 1, cancellationToken: cancellationToken);
        return await BuildTradeUpRecipeSummary(userId, cancellationToken);
    }

    private async Task<CaseOpeningTradeUpRecipeSummaryObj> BuildTradeUpRecipeSummary(Guid userId, CancellationToken cancellationToken)
    {
        List<CaseOpeningTradeUpRecipeObj> recipes = await _data.GetCaseOpeningTradeUpRecipes(userId, cancellationToken);
        List<CaseOpeningTradeUpHoldingObj> holdings = await _data.GetCaseOpeningTradeUpHoldings(userId, cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        recipes.ForEach(recipe => recipe.HoldingUpgradeCost = recipe.HoldingCapacity >= MaximumTradeUpRecipeHoldingCapacity
            ? 0
            : IsGbp(settings)
                ? settings.TradeUpHoldingUpgradeBaseCostGbpPence + (recipe.HoldingCapacity * settings.TradeUpHoldingUpgradeCostIncrementGbpPence)
                : settings.TradeUpHoldingUpgradeBaseCostStars + (recipe.HoldingCapacity * settings.TradeUpHoldingUpgradeCostIncrementStars));
        return new CaseOpeningTradeUpRecipeSummaryObj
        {
            UsedRecipeSlots = recipes.Count(recipe => recipe.IsActive),
            UsedHoldingCount = holdings.Count,
            RecipeCostStars = settings.TradeUpRecipeCostStars,
            RecipeCost = IsGbp(settings) ? settings.TradeUpRecipeCostGbpPence : settings.TradeUpRecipeCostStars,
            EconomyMode = settings.EconomyMode,
            ActiveBalanceMinor = IsGbp(settings) ? progress.GbpPence : progress.Stars,
            Recipes = recipes,
            Holdings = holdings
        };
    }

    /// <summary>
    /// The server decides the result before the animation starts. This keeps the displayed reel
    /// honest and prevents browser code from selecting or replacing the winning item.
    /// </summary>
    public async Task<CaseOpeningOpenBatchResultObj> OpenCases(
        Guid userId,
        string caseKey,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        if (quantity < 1 || quantity > settings.MaximumOpenQuantity)
        {
            throw new InvalidOperationException($"Open between 1 and {settings.MaximumOpenQuantity} cases at a time.");
        }

        CaseOpeningProgressDbModel progress = await _data.GetCaseOpeningProgress(userId, cancellationToken);
        int availableQuantity = 1 + progress.MultiOpenLevel;
        if (quantity > availableQuantity)
        {
            throw new InvalidOperationException($"Unlock more Multi case opening levels before opening more than {availableQuantity} cases at a time.");
        }

        List<string> unlockedCaseKeys = await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken);
        if (!unlockedCaseKeys.Contains(caseKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unlock this case before opening it.");
        }

        int ownedQuantity = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .FirstOrDefault(item => item.CaseKey.Equals(caseKey, StringComparison.OrdinalIgnoreCase))?.Quantity ?? 0;
        if (ownedQuantity < quantity)
        {
            throw new InvalidOperationException($"You own {ownedQuantity} {caseKey} case{(ownedQuantity == 1 ? string.Empty : "s")}. Buy more from the Shop when it is available.");
        }

        // Each result replaces the owned case that produced it, so opening is capacity-neutral.
        // The stored procedure performs that exchange atomically for every item in the batch.
        Dictionary<string, int> xpByRarity = await GetXpByRarityByKey(cancellationToken);
        CaseOpeningInventoryUpgradeDbModel autoSellSettings = await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> saleSettings = await GetCaseSettingsByKey(cancellationToken);
        HashSet<string> forcedRarityKeys = await GetDevForcedRarityKeys(userId, cancellationToken);
        List<CaseOpeningResultObj> results = [];
        for (int index = 0; index < quantity; index++)
        {
            results.Add(await OpenCase(
                userId,
                caseKey,
                cancellationToken,
                xpByRarity,
                settings.XpPerCaseOpen,
                autoSellSettings,
                saleSettings,
                settings,
                deferPostOpenEvaluation: true,
                forcedRarityKeys: forcedRarityKeys));
        }

        // These inspect account-wide state and were previously repeated for every result in a
        // multi-open. Five pulls therefore performed the same collection and achievement scans
        // five times before the browser could receive its one batch response.
        CaseOpeningCaseObj openedCase = await _referenceData.GetCase(caseKey, cancellationToken);
        await RecordCollectionMilestones(userId, openedCase, cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
        await NotifyLiveWinnersChanged(cancellationToken);

        int remainingQuantity = (await _data.GetCaseOpeningOwnedCases(userId, cancellationToken))
            .FirstOrDefault(item => item.CaseKey.Equals(caseKey, StringComparison.OrdinalIgnoreCase))?.Quantity ?? 0;
        return new CaseOpeningOpenBatchResultObj
        {
            Results = results,
            RemainingCaseQuantity = remainingQuantity
        };
    }

    private async Task<Dictionary<string, int>> GetXpByRarityByKey(CancellationToken cancellationToken)
    {
        List<CaseOpeningXpByRarityObj> xpByRarity = await _data.GetXpByRarity(cancellationToken);
        return xpByRarity.ToDictionary(item => item.RarityKey, item => item.XpAwarded, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<CaseOpeningResultObj> OpenCase(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken,
        Dictionary<string, int>? xpByRarity = null,
        int fallbackXp = 5,
        CaseOpeningInventoryUpgradeDbModel? autoSellSettings = null,
        Dictionary<string, CaseOpeningCaseSettingsObj>? caseSaleSettings = null,
        CaseOpeningGameSettingsObj? gameSettings = null,
        bool deferPostOpenEvaluation = false,
        ISet<string>? forcedRarityKeys = null)
    {
        ValidateCaseKey(caseKey);
        CaseOpeningCaseObj caseData = await _referenceData.GetCase(caseKey, cancellationToken);
        List<CaseOpeningOddsObj> availableOdds = caseData.Odds
            .Where(odd => caseData.Items.Any(item => item.RarityKey == odd.RarityKey))
            .ToList();
        if (availableOdds.Count == 0)
            throw new InvalidOperationException("The case contents could not be loaded. Please try again shortly.");

        if (forcedRarityKeys is { Count: > 0 })
        {
            List<CaseOpeningOddsObj> forcedOdds = availableOdds
                .Where(odd => forcedRarityKeys.Contains(odd.RarityKey))
                .ToList();
            if (forcedOdds.Count > 0) availableOdds = forcedOdds;
        }

        // If a public source changes a catalogue, only roll its still-present tiers. This avoids
        // an intermittent failed opening and preserves the configured relative odds.
        string rarityKey = SelectRarity(availableOdds);
        List<CaseOpeningItemObj> eligible = caseData.Items.Where(item => item.RarityKey == rarityKey).ToList();

        if (eligible.Count == 0)
        {
            throw new InvalidOperationException("The case contents could not be loaded. Please try again shortly.");
        }

        CaseOpeningItemObj winner = Clone(eligible[RandomNumberGenerator.GetInt32(eligible.Count)]);
        await ApplyUniqueCondition(userId, winner, caseData.Type, cancellationToken);
        CaseOpeningSpecialVariantRuleDbModel? specialVariant = await ResolveSpecialVariant(winner, cancellationToken);
        if (specialVariant is not null)
        {
            winner.SpecialVariantRuleId = specialVariant.RuleId;
            winner.SpecialVariantName = specialVariant.Name;
            winner.SpecialVariantTier = specialVariant.Tier;
            winner.SpecialVariantDescription = specialVariant.Description;
            winner.SpecialVariantPriceSnapshotId = specialVariant.PriceSnapshotId;
            winner.SpecialVariantPrice = specialVariant.Price;
        }
        winner.EstimatedPrice = await _prices.GetEstimatedPrice(winner.MarketHashName, cancellationToken);
        if (winner.SpecialVariantPrice is not null) winner.EstimatedPrice = winner.SpecialVariantPrice;
        decimal? marketOpeningCost = await _prices.GetEstimatedPrice(caseData.Name, cancellationToken);

        CaseOpeningHistoryDbModel history = winner.Adapt<CaseOpeningHistoryDbModel>();
        history.OpeningId = Guid.NewGuid();
        history.UserId = userId;
        history.CaseKey = caseKey;
        history.OpenedUtc = DateTime.UtcNow;
        bool isNewCollectionItem = !await _data.CaseOpeningCollectionItemExists(userId, caseKey, history.SourceItemId, cancellationToken);
        await _data.SaveCaseOpening(userId, history, cancellationToken);
        if (specialVariant is not null) await _data.SaveCaseOpeningSpecialVariant(history.OpeningId, specialVariant, cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> resolvedSaleSettings = caseSaleSettings ?? await GetCaseSettingsByKey(cancellationToken);
        (int caseUnlockCost, _, _) = GetCaseSettings(resolvedSaleSettings, caseKey);
        int pullValueStars = GetSaleValue(rarityKey) * GetCaseSaleMultiplier(caseUnlockCost);
        await RecordPlayerActivity(userId, casesOpened: 1, skinsObtained: 1, rarityKey: rarityKey, isStatTrak: winner.IsStatTrak, pullValueStars: pullValueStars, cancellationToken: cancellationToken);
        if (!deferPostOpenEvaluation)
        {
            await RecordCollectionMilestones(userId, caseData, cancellationToken);
            await EvaluateAchievements(userId, cancellationToken);
        }

        Dictionary<string, int> resolvedXpByRarity = xpByRarity ?? await GetXpByRarityByKey(cancellationToken);
        int xpAward = resolvedXpByRarity.TryGetValue(rarityKey, out int rarityXp) ? rarityXp : fallbackXp;
        CaseOpeningProgressDbModel? afterXp = await _data.AddCaseOpeningXp(userId, xpAward, cancellationToken);
        // Daily progress is independently capped by its procedure and resets by UTC date. It is
        // intentionally awarded from the same server-side XP value as the player level.
        int focusLevel = (await _data.GetCaseOpeningDailyDropUpgrades(userId, cancellationToken))
            .FirstOrDefault(item => item.UpgradeKey.Equals("focus", StringComparison.OrdinalIgnoreCase))?.Level ?? 0;
        int dailyRequiredXp = Math.Max(40, DailyDropRequiredXp - (focusLevel * 10));
        await _data.AddCaseOpeningDailyDropXp(userId, xpAward, dailyRequiredXp, cancellationToken);
        int totalXp = afterXp?.Xp ?? 0;
        int newLevel = CaseOpeningXpLevels.GetLevel(totalXp);
        int previousLevel = CaseOpeningXpLevels.GetLevel(totalXp - xpAward);
        int levelRewardStars = 0;
        if (newLevel > previousLevel)
        {
            // Only reward levels crossed by this opening. This avoids retroactively awarding a
            // large balance to existing accounts when the progression foundation first ships.
            int reward = CaseOpeningXpLevels.GetRewardStarsBetween(previousLevel, newLevel);
            if (reward > 0 && await _data.ClaimCaseOpeningLevelReward(userId, newLevel, reward, reward * 5L, cancellationToken))
            {
                levelRewardStars = reward;
                await RecordPlayerActivity(userId, levelRewardStars: reward, cancellationToken: cancellationToken);
            }
        }

        CaseOpeningInventoryUpgradeDbModel autoSell = autoSellSettings ?? await _data.GetCaseOpeningInventoryUpgrades(userId, cancellationToken);
        string autoSellKey = rarityKey is "high-grade" ? "mil-spec" : rarityKey is "remarkable" ? "restricted" : rarityKey is "exotic" ? "classified" : rarityKey;
        bool autoSellEnabled = autoSellKey switch
        {
            "covert" => autoSell.AutoSellCovertUnlocked && autoSell.AutoSellCovertEnabled,
            "classified" => autoSell.AutoSellClassifiedUnlocked && autoSell.AutoSellClassifiedEnabled,
            "restricted" => autoSell.AutoSellRestrictedUnlocked && autoSell.AutoSellRestrictedEnabled,
            "mil-spec" => autoSell.AutoSellMilSpecUnlocked && autoSell.AutoSellMilSpecEnabled,
            _ => false
        };
        bool isAutoSold = autoSellEnabled && (!winner.IsStatTrak || !autoSell.PreserveStatTrak);
        int autoSoldStars = 0;
        if (isAutoSold)
        {
            CaseOpeningGameSettingsObj economySettings = gameSettings ?? await _data.GetGameSettings(cancellationToken);
            (int unlockCost, _, _) = GetCaseSettings(resolvedSaleSettings, caseKey);
            int fallbackStars = GetSaleValue(rarityKey) * GetCaseSaleMultiplier(unlockCost);
            CaseOpeningSaleAward autoSaleAward = CaseOpeningBalancePolicy.CalculateSaleAward(history.EstimatedPrice, economySettings.SkinSaleRateBasisPoints, fallbackStars);
            autoSoldStars = autoSaleAward.Stars;
            long autoSoldGbpPence = autoSaleAward.GbpPence;
            await _data.SellCaseOpeningInventory(userId, [history.OpeningId], autoSoldStars, autoSoldGbpPence, cancellationToken);
            await RecordPlayerActivity(userId, saleStarsEarned: autoSoldStars, cancellationToken: cancellationToken);
        }

        const int winnerIndex = 31;
        List<CaseOpeningItemObj> reel = Enumerable.Range(0, 38)
            // The visible reel must resemble the published rarity odds. Choosing uniformly from
            // every skin heavily over-represents cases with large knife or glove pools.
            .Select(_ => SelectReelItem(caseData))
            .ToList();
        reel[winnerIndex] = winner;

        return new CaseOpeningResultObj
        {
            OpeningId = history.OpeningId,
            CaseKey = caseKey,
            CaseName = caseData.Name,
            Winner = winner,
            Reel = reel,
            WinnerIndex = winnerIndex,
            XpAwarded = xpAward,
            TotalXp = totalXp,
            Level = newLevel,
            LeveledUp = newLevel > previousLevel,
            LevelRewardStars = levelRewardStars,
            LevelRewardAmountMinor = IsGbp(gameSettings ?? await _data.GetGameSettings(cancellationToken)) ? levelRewardStars * 5L : levelRewardStars,
            IsAutoSold = isAutoSold,
            AutoSoldStars = autoSoldStars,
            IsNewCollectionItem = isNewCollectionItem,
            MarketOpeningCost = marketOpeningCost,
            MarketProfit = winner.EstimatedPrice is not null && marketOpeningCost is not null
                ? decimal.Round(winner.EstimatedPrice.Value - marketOpeningCost.Value, 2)
                : null
        };
    }

    /// <summary>
    /// Collection milestones represent permanent progress, not the current inventory. A sold
    /// skin therefore remains collected and can still contribute to completion achievements.
    /// </summary>
    private async Task RecordCollectionMilestones(
        Guid userId,
        CaseOpeningCaseObj caseData,
        CancellationToken cancellationToken)
    {
        try
        {
            List<CaseOpeningCollectionDbModel> collectedItems = await _data.GetCaseOpeningCollection(
                userId,
                caseData.CaseKey,
                cancellationToken);
            HashSet<string> collectedSourceIds = collectedItems
                .Select(item => item.SourceItemId)
                .ToHashSet(StringComparer.Ordinal);

            foreach (IGrouping<string, CaseOpeningItemObj> rarity in caseData.Items.GroupBy(item => item.RarityKey))
            {
                bool rarityComplete = rarity.Key.Equals("rare-special", StringComparison.OrdinalIgnoreCase)
                    ? rarity.Any(item => collectedSourceIds.Contains(item.SourceItemId))
                    : rarity.All(item => collectedSourceIds.Contains(item.SourceItemId));

                if (rarityComplete)
                {
                    await _data.RecordCompletedCaseOpeningRarity(userId, caseData.CaseKey, rarity.Key, cancellationToken);
                }
            }

            bool normalItemsComplete = caseData.Items
                .Where(item => !item.IsRareSpecial)
                .All(item => collectedSourceIds.Contains(item.SourceItemId));
            bool rareObjectiveComplete = !caseData.Items.Any(item => item.IsRareSpecial)
                || caseData.Items.Any(item => item.IsRareSpecial && collectedSourceIds.Contains(item.SourceItemId));

            if (normalItemsComplete && rareObjectiveComplete)
            {
                await _data.RecordCompletedCaseOpeningCollection(userId, caseData.CaseKey, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            // The skin is already safely stored. Do not turn a successful opening into a browser
            // error solely because an optional milestone update needs a retry on a future pull.
            _logger.LogWarning(
                exception,
                "Case-opening collection milestone update failed for {UserId} and {CaseKey}.",
                userId,
                caseData.CaseKey);
        }
    }

    private async Task RecordPlayerActivity(
        Guid userId,
        int casesOpened = 0,
        int skinsObtained = 0,
        int tradeUpsCompleted = 0,
        int unlocksEarned = 0,
        string rarityKey = "",
        bool isStatTrak = false,
        int casesPurchased = 0,
        int casePurchaseStarsSpent = 0,
        int saleStarsEarned = 0,
        int pullValueStars = 0,
        int starsSpent = 0,
        int levelRewardStars = 0,
        int upgradesPurchased = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _data.RecordCaseOpeningPlayerActivity(
                userId,
                casesOpened,
                skinsObtained,
                tradeUpsCompleted,
                unlocksEarned,
                rarityKey,
                isStatTrak,
                casesPurchased,
                casePurchaseStarsSpent,
                saleStarsEarned,
                pullValueStars,
                starsSpent,
                levelRewardStars,
                upgradesPurchased,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Opening, selling and unlocking already completed in their own stored procedure.
            // Keep the simulator responsive and retain a clear server log for the missed metric.
            _logger.LogWarning(exception, "Case-opening progression activity could not be recorded for {UserId}.", userId);
        }
    }

    private async Task RecordLoginAndEvaluateAchievements(Guid userId, CancellationToken cancellationToken)
    {
        await _data.RecordCaseOpeningLogin(userId, cancellationToken);
        await EvaluateAchievements(userId, cancellationToken);
    }

    private async Task EvaluateAchievements(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _data.EvaluateCaseOpeningAchievements(userId, cancellationToken);
        }
        catch (Exception exception)
        {
            // Achievements are an enhancement over the completed opening/login operation. Keep
            // that primary action successful while retaining enough context to retry and diagnose.
            _logger.LogWarning(exception, "Case-opening achievements could not be evaluated for {UserId}.", userId);
        }
    }

    private static int GetAchievementMetricValue(CaseOpeningPlayerStatsDbModel stats, string metricKey)
    {
        return metricKey switch
        {
            "cases-opened" => stats.TotalCasesOpened,
            "skins-obtained" => stats.TotalSkinsObtained,
            "trade-ups-completed" => stats.TotalTradeUpsCompleted,
            "unlocks" => stats.TotalUnlocks,
            "login-days" => stats.TotalLoginDays,
            "login-streak" => stats.CurrentLoginStreak,
            "collections-completed" => stats.CompletedCollections,
            "rarity-sets-completed" => stats.CompletedRaritySets,
            _ => 0
        };
    }

    public Task<CaseOpeningGameSettingsObj> GetGameSettings(CancellationToken cancellationToken = default)
    {
        return _data.GetGameSettings(cancellationToken);
    }

    public async Task<CaseOpeningGameSettingsObj> SetGameSettings(CaseOpeningGameSettingsObj settings, CancellationToken cancellationToken = default)
    {
        NormalizeStarDisplaysFromGbp(settings);
        ValidateGameSettings(settings);
        await _data.SetGameSettings(settings, cancellationToken);
        return await _data.GetGameSettings(cancellationToken);
    }

    public Task<List<CaseOpeningCaseSettingsObj>> GetCaseSettings(CancellationToken cancellationToken = default)
    {
        return _data.GetCaseSettings(cancellationToken);
    }

    public Task SetCaseSettings(string caseKey, int tier, int unlockCostStars, long unlockCostGbpPence, int purchaseCostStars, long purchaseCostGbpPence, int xpRequirement, CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        if (tier is < 1 or > 10 || unlockCostStars < 0 || unlockCostGbpPence < 0 || purchaseCostStars < 0 || purchaseCostGbpPence < 0 || xpRequirement < 0)
        {
            throw new InvalidOperationException("Costs and XP requirements cannot be negative.");
        }

        return _data.SetCaseSettings(caseKey, tier, StarsFromPence(unlockCostGbpPence), unlockCostGbpPence, StarsFromPence(purchaseCostGbpPence), purchaseCostGbpPence, xpRequirement, cancellationToken);
    }

    public Task<List<CaseOpeningUpgradeDefinitionObj>> GetInventoryUpgradeSettings(CancellationToken cancellationToken = default)
    {
        return _data.GetInventoryUpgradeSettings(cancellationToken);
    }

    public async Task SetInventoryUpgradeSettings(string upgradeKey, int costStars, long costGbpPence, int requiredLevel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upgradeKey) || costStars < 0 || costGbpPence < 0 || requiredLevel < 0)
        {
            throw new InvalidOperationException("Upgrade costs and level requirements must be valid non-negative values.");
        }

        List<CaseOpeningUpgradeDefinitionObj> definitions = await _data.GetInventoryUpgradeSettings(cancellationToken);
        if (!definitions.Any(item => string.Equals(item.UpgradeKey, upgradeKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("This inventory upgrade could not be found.");
        }

        await _data.SetInventoryUpgradeSettings(upgradeKey, StarsFromPence(costGbpPence), costGbpPence, requiredLevel, cancellationToken);
    }

    private Task NotifyLiveWinnersChanged(CancellationToken cancellationToken) =>
        _liveWinners.Clients.All.SendAsync("winnersChanged", cancellationToken);

    private async Task<CaseOpeningSpecialVariantRuleDbModel?> ResolveSpecialVariant(CaseOpeningItemObj item, CancellationToken cancellationToken)
    {
        List<CaseOpeningSpecialVariantRuleDbModel> rules = await _data.GetActiveCaseOpeningSpecialVariantRules(cancellationToken);
        return rules.FirstOrDefault(rule =>
            string.Equals(rule.SourceItemId, item.SourceItemId, StringComparison.Ordinal)
            && (string.IsNullOrEmpty(rule.PaintIndex) || string.Equals(rule.PaintIndex, item.PaintIndex, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrEmpty(rule.Phase) || string.Equals(rule.Phase, item.Phase, StringComparison.OrdinalIgnoreCase))
            && (rule.PatternSeed is null || rule.PatternSeed == item.PatternSeed)
            && (rule.MinimumFloat is null || item.FloatValue >= rule.MinimumFloat)
            && (rule.MaximumFloat is null || item.FloatValue <= rule.MaximumFloat)
            && (rule.RequiresStatTrak is null || rule.RequiresStatTrak == item.IsStatTrak));
    }

    public async Task<CaseOpeningPriceSnapshotSummaryObj> GetPriceSnapshots(CancellationToken cancellationToken = default)
    {
        List<CaseOpeningPriceSnapshotDbModel> snapshots = await _prices.GetSnapshots(cancellationToken);
        List<CaseOpeningSnapshotPriceDbModel> prices = await _prices.GetActivePrices(cancellationToken);
        Dictionary<string, decimal> priceByName = prices.ToDictionary(item => item.MarketHashName, item => item.Price, StringComparer.Ordinal);
        List<CaseOpeningCaseObj> catalogue = await _referenceData.GetCuratedCases(cancellationToken);
        Dictionary<string, CaseOpeningCaseSettingsObj> settings = await GetCaseSettingsByKey(cancellationToken);
        CaseOpeningGameSettingsObj gameSettings = await _data.GetGameSettings(cancellationToken);
        List<CaseOpeningCaseMarketValueObj> cases = catalogue.Select(item =>
        {
            CaseOpeningCaseSettingsObj configured = settings.GetValueOrDefault(item.CaseKey) ?? new CaseOpeningCaseSettingsObj { CaseKey = item.CaseKey };
            item.Tier = configured.Tier;
            CaseOpeningCaseMarketValueObj value = BuildCaseMarketValue(item, priceByName);
            value.Tier = configured.Tier;
            value.PublishedPurchaseGbpPence = configured.PurchaseCostGbpPence;
            value.HasCompletePricing = value.ExpectedValue is not null;
            if (value.ExpectedValue is decimal expectedValue)
            {
                CaseOpeningBalanceRecommendation recommendation = CaseOpeningBalancePolicy.RecommendCasePrices(expectedValue, configured.Tier, gameSettings.SkinSaleRateBasisPoints);
                value.ExpectedSaleValuePence = recommendation.ExpectedSaleValuePence;
                value.TargetReturnPercentage = recommendation.TargetReturnPercentage;
                value.RecommendedPurchaseGbpPence = recommendation.RecommendedPurchaseGbpPence;
                value.RecommendedUnlockGbpPence = recommendation.RecommendedUnlockGbpPence;
                value.RecommendedPurchaseStars = recommendation.RecommendedPurchaseStars;
                value.RecommendedUnlockStars = recommendation.RecommendedUnlockStars;
            }
            return value;
        }).ToList();
        List<string> tierWarnings = CaseOpeningEconomyPolicy.ValidateTierCoverage(cases.Select(item => item.Tier));
        return new CaseOpeningPriceSnapshotSummaryObj
        {
            Snapshots = snapshots,
            ActiveSnapshotId = snapshots.FirstOrDefault(item => item.IsActive)?.PriceSnapshotId,
            Currency = snapshots.FirstOrDefault(item => item.IsActive)?.Currency ?? "GBP",
            Cases = cases,
            CanPublish = snapshots.Any(item => item.IsActive) && cases.All(item => item.HasCompletePricing) && tierWarnings.Count == 0,
            FallbackPriceCount = prices.Count(item => item.IsFallback),
            MissingPriceCount = cases.Sum(item => Math.Max(0, item.TotalVariants - item.PricedVariants)),
            TierWarnings = tierWarnings
        };
    }

    public async Task<CaseOpeningSpecialVariantAdminSummaryObj> GetSpecialVariantSettings(CancellationToken cancellationToken = default)
    {
        List<CaseOpeningSpecialVariantRuleDbModel> rules = await _data.GetCaseOpeningSpecialVariantRules(cancellationToken);
        List<CaseOpeningSpecialVariantPriceSnapshotObj> snapshots = await _data.GetCaseOpeningSpecialVariantPriceSnapshots(cancellationToken);
        return new CaseOpeningSpecialVariantAdminSummaryObj { Rules = rules, Snapshots = snapshots, ActiveSnapshotId = snapshots.FirstOrDefault(item => item.IsActive)?.PriceSnapshotId };
    }

    public async Task<CaseOpeningSpecialVariantAdminSummaryObj> SaveSpecialVariantRule(Guid? ruleId, CaseOpeningSpecialVariantRuleRequestObj rule, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rule.SourceItemId) || string.IsNullOrWhiteSpace(rule.MarketHashName) || string.IsNullOrWhiteSpace(rule.Name) || string.IsNullOrWhiteSpace(rule.Tier) || string.IsNullOrWhiteSpace(rule.Description)
            || rule.PatternSeed is < 0 or > 1000 || rule.MinimumFloat is < 0 or > 1 || rule.MaximumFloat is < 0 or > 1
            || rule.MinimumFloat is not null && rule.MaximumFloat is not null && rule.MinimumFloat > rule.MaximumFloat)
            throw new InvalidOperationException("Provide an item ID, name, tier, description, and valid optional pattern/float criteria.");
        await _data.SaveCaseOpeningSpecialVariantRule(ruleId ?? Guid.NewGuid(), rule, cancellationToken);
        return await GetSpecialVariantSettings(cancellationToken);
    }

    public async Task<CaseOpeningSpecialVariantAdminSummaryObj> CreateSpecialVariantPriceSnapshot(CaseOpeningSpecialVariantPriceSnapshotRequestObj request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Source) || request.Prices.Count == 0 || request.Prices.Any(item => item.Value < 0))
            throw new InvalidOperationException("Provide a snapshot name, source, and at least one non-negative rule price.");
        await _data.CreateCaseOpeningSpecialVariantPriceSnapshot(new CaseOpeningSpecialVariantPriceSnapshotObj { PriceSnapshotId = Guid.NewGuid(), Name = request.Name.Trim(), Source = request.Source.Trim(), ImportedUtc = DateTime.UtcNow }, request.Prices, cancellationToken);
        return await GetSpecialVariantSettings(cancellationToken);
    }

    public async Task<CaseOpeningSpecialVariantAdminSummaryObj> ActivateSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _data.ActivateCaseOpeningSpecialVariantPriceSnapshot(snapshotId, cancellationToken);
        return await GetSpecialVariantSettings(cancellationToken);
    }

    public async Task<CaseOpeningSpecialVariantAdminSummaryObj> DeleteSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _data.DeleteCaseOpeningSpecialVariantPriceSnapshot(snapshotId, cancellationToken);
        return await GetSpecialVariantSettings(cancellationToken);
    }

    public async Task<List<CaseOpeningSpecialVariantListingEvidenceObj>> ImportSpecialVariantListings(Guid userId, CancellationToken cancellationToken = default)
    {
        string? key = await _settings.GetSecret(userId, AppSettingKey.CSFloatApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Save a CSFloat API key in Settings before importing listing evidence.");
        List<CaseOpeningSpecialVariantRuleDbModel> rules = await _data.GetCaseOpeningSpecialVariantRules(cancellationToken);
        List<CaseOpeningSpecialVariantListingEvidenceObj> result = [];
        foreach (CaseOpeningSpecialVariantRuleDbModel rule in rules.Where(rule => !string.IsNullOrWhiteSpace(rule.MarketHashName)))
            result.AddRange(await _csFloat.GetListings(rule, key, cancellationToken));
        return result;
    }

    public async Task<CaseOpeningCaseDiscardResultObj> DiscardCaseOpeningCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        await _referenceData.GetCase(caseKey, cancellationToken);
        if (quantity is not (0 or 10 or 25 or 50 or 100))
        {
            throw new InvalidOperationException("Choose 10, 25, 50, 100, or all cases to discard.");
        }

        CaseOpeningCaseDiscardResultObj? result = await _data.DiscardCaseOpeningCases(userId, caseKey, quantity, cancellationToken);
        return result ?? throw new InvalidOperationException("Those cases could not be discarded. Please try again.");
    }

    public async Task<CaseOpeningPriceSnapshotSummaryObj> CreatePriceSnapshot(CancellationToken cancellationToken = default)
    {
        List<CaseOpeningCaseObj> catalogue = await _referenceData.GetCuratedCases(cancellationToken);
        List<CaseOpeningMarketPriceTarget> targets = catalogue
            .SelectMany(caseData => caseData.Items.SelectMany(item => MarketVariants(item, caseData.Type)
                .Select(variant => new CaseOpeningMarketPriceTarget(variant.Name, caseData.CaseKey, item.RarityKey, item.IsRareSpecial))))
            .ToList();
        await _prices.CreateSkinportSnapshot(targets, cancellationToken);
        return await GetPriceSnapshots(cancellationToken);
    }

    public async Task<CaseOpeningPriceSnapshotSummaryObj> ActivatePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _prices.ActivateSnapshot(snapshotId, cancellationToken);
        return await GetPriceSnapshots(cancellationToken);
    }

    public async Task<CaseOpeningPriceSnapshotSummaryObj> DeletePriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _prices.DeleteSnapshot(snapshotId, cancellationToken);
        return await GetPriceSnapshots(cancellationToken);
    }

    public async Task<CaseOpeningPriceSnapshotSummaryObj> PublishPriceSnapshotBalance(CancellationToken cancellationToken = default)
    {
        CaseOpeningPriceSnapshotSummaryObj draft = await GetPriceSnapshots(cancellationToken);
        if (draft.ActiveSnapshotId is null) throw new InvalidOperationException("Create or activate a price snapshot before publishing a balance.");
        List<CaseOpeningCaseMarketValueObj> incomplete = draft.Cases.Where(item => !item.HasCompletePricing).ToList();
        if (incomplete.Count > 0) throw new InvalidOperationException($"Pricing is incomplete for {incomplete.Count} case{(incomplete.Count == 1 ? string.Empty : "s")}. Missing prices must be resolved before publishing.");
        if (draft.TierWarnings.Count > 0) throw new InvalidOperationException(string.Join(" ", draft.TierWarnings));
        Dictionary<string, CaseOpeningCaseSettingsObj> settings = await GetCaseSettingsByKey(cancellationToken);
        foreach (CaseOpeningCaseMarketValueObj item in draft.Cases)
        {
            CaseOpeningCaseSettingsObj current = settings[item.CaseKey];
            await _data.SetCaseSettings(item.CaseKey, current.Tier, item.RecommendedUnlockStars, item.RecommendedUnlockGbpPence, item.RecommendedPurchaseStars, item.RecommendedPurchaseGbpPence, current.XpRequirement, cancellationToken);
        }
        return await GetPriceSnapshots(cancellationToken);
    }

    public Task<List<CaseOpeningXpByRarityObj>> GetXpByRarity(CancellationToken cancellationToken = default)
    {
        return _data.GetXpByRarity(cancellationToken);
    }

    public Task SetXpByRarity(string rarityKey, int xpAwarded, CancellationToken cancellationToken = default)
    {
        if (!SaleValues.ContainsKey(rarityKey))
        {
            throw new InvalidOperationException("This is not a recognised rarity.");
        }

        if (xpAwarded < 0)
        {
            throw new InvalidOperationException("XP awarded cannot be negative.");
        }

        return _data.SetXpByRarity(rarityKey, xpAwarded, cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> SetDevProgress(Guid userId, int stars, long gbpPence, int xp, CancellationToken cancellationToken = default)
    {
        if (stars < 0 || gbpPence < 0 || xp < 0)
        {
            throw new InvalidOperationException("Balances and XP cannot be negative.");
        }

        CaseOpeningProgressDbModel? updated = await _data.SetCaseOpeningProgressDev(userId, stars, gbpPence, xp, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("Your progress could not be updated. Please try again.");
        }

        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        return await BuildProgress(updated, settings, await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken), cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> SetDevUpgrades(Guid userId, bool skipAnimationUnlocked, int multiOpenLevel, int openSpeedLevel, CancellationToken cancellationToken = default)
    {
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        if (multiOpenLevel < 0 || multiOpenLevel > settings.MaximumMultiOpenLevel)
        {
            throw new InvalidOperationException($"Multi-open level must be between 0 and {settings.MaximumMultiOpenLevel}.");
        }

        if (openSpeedLevel < 0 || openSpeedLevel > settings.MaximumOpenSpeedLevel)
        {
            throw new InvalidOperationException($"Speed level must be between 0 and {settings.MaximumOpenSpeedLevel}.");
        }

        CaseOpeningProgressDbModel? updated = await _data.SetCaseOpeningUpgradesDev(userId, skipAnimationUnlocked, multiOpenLevel, openSpeedLevel, cancellationToken);
        if (updated is null)
        {
            throw new InvalidOperationException("Your upgrades could not be updated. Please try again.");
        }

        return await BuildProgress(updated, settings, await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken), cancellationToken);
    }

    public async Task<CaseOpeningProgressObj> SetDevCaseUnlock(Guid userId, string caseKey, bool unlock, CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        await _data.SetCaseOpeningCaseUnlockDev(userId, caseKey, unlock, cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        return await BuildProgress(
            await _data.GetCaseOpeningProgress(userId, cancellationToken),
            settings,
            await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken),
            cancellationToken);
    }

    public async Task<CaseOpeningDevDropSettingsObj> GetDevDropSettings(Guid userId, CancellationToken cancellationToken = default)
    {
        return new CaseOpeningDevDropSettingsObj
        {
            RarityGroups = NormaliseDevDropRarityGroups(await _data.GetCaseOpeningDevDropRarityGroups(userId, cancellationToken))
        };
    }

    public async Task<CaseOpeningDevDropSettingsObj> SetDevDropSettings(Guid userId, IEnumerable<string>? rarityGroups, CancellationToken cancellationToken = default)
    {
        List<string> groups = NormaliseDevDropRarityGroups(rarityGroups);
        await _data.SetCaseOpeningDevDropRarityGroups(userId, groups, cancellationToken);
        return new CaseOpeningDevDropSettingsObj { RarityGroups = groups };
    }

    public async Task<CaseOpeningProgressObj> ResetDevProgress(Guid userId, CancellationToken cancellationToken = default)
    {
        await _data.ResetCaseOpeningProgressDev(userId, cancellationToken);
        await _data.SetCaseOpeningDevDropRarityGroups(userId, [], cancellationToken);
        CaseOpeningGameSettingsObj settings = await _data.GetGameSettings(cancellationToken);
        return await BuildProgress(
            await _data.GetCaseOpeningProgress(userId, cancellationToken),
            settings,
            await _data.GetCaseOpeningUnlockedCases(userId, cancellationToken),
            cancellationToken);
    }

    private async Task<HashSet<string>> GetDevForcedRarityKeys(Guid userId, CancellationToken cancellationToken)
    {
        return NormaliseDevDropRarityGroups(await _data.GetCaseOpeningDevDropRarityGroups(userId, cancellationToken))
            .SelectMany(group => DevDropRarityKeys[group])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> NormaliseDevDropRarityGroups(IEnumerable<string>? rarityGroups)
    {
        string[] order = ["blue", "purple", "pink", "red", "gold"];
        return (rarityGroups ?? [])
            .Select(group => group?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(DevDropRarityKeys.ContainsKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => Array.IndexOf(order, group))
            .ToList();
    }

    private async Task<Dictionary<string, CaseOpeningCaseSettingsObj>> GetCaseSettingsByKey(CancellationToken cancellationToken)
    {
        List<CaseOpeningCaseSettingsObj> settings = await _data.GetCaseSettings(cancellationToken);
        return settings.ToDictionary(item => item.CaseKey, StringComparer.OrdinalIgnoreCase);
    }

    private static (int Cost, int PurchaseCost, int XpRequirement) GetCaseSettings(Dictionary<string, CaseOpeningCaseSettingsObj> caseSettings, string caseKey)
    {
        return caseSettings.TryGetValue(caseKey, out CaseOpeningCaseSettingsObj? settings)
            ? (settings.UnlockCostStars, settings.PurchaseCostStars, settings.XpRequirement)
            : (0, 0, 0);
    }

    private async Task<CaseOpeningProgressObj> BuildProgress(
        CaseOpeningProgressDbModel progress,
        CaseOpeningGameSettingsObj settings,
        List<string>? unlockedCaseKeys,
        CancellationToken cancellationToken)
    {
        Dictionary<string, CaseOpeningCaseSettingsObj> caseSettings = await GetCaseSettingsByKey(cancellationToken);
        CaseOpeningProgressObj result = CreateProgress(progress, settings, unlockedCaseKeys);
        CaseOpeningDailyDropDbModel daily = await _data.GetCaseOpeningDailyDrop(progress.UserId, cancellationToken);
        Dictionary<string, int> dailyUpgradeLevels = (await _data.GetCaseOpeningDailyDropUpgrades(progress.UserId, cancellationToken)).ToDictionary(item => item.UpgradeKey, item => item.Level, StringComparer.OrdinalIgnoreCase);
        if (daily.IsCompleted && !daily.IsClaimed && string.IsNullOrWhiteSpace(daily.OfferJson))
        {
            List<CaseOpeningDailyDropRewardObj> offer = await CreateDailyDropOffer(settings, unlockedCaseKeys ?? [], dailyUpgradeLevels, cancellationToken);
            daily = await _data.SetCaseOpeningDailyDropOffer(progress.UserId, JsonSerializer.Serialize(offer), cancellationToken);
        }
        result.DailyDrop = new CaseOpeningDailyDropObj
        {
            DropDate = daily.DropDate,
            Xp = daily.Xp,
            RequiredXp = Math.Max(40, DailyDropRequiredXp - (dailyUpgradeLevels.GetValueOrDefault("focus") * 10)),
            IsCompleted = daily.IsCompleted,
            IsClaimed = daily.IsClaimed,
            Rewards = ParseDailyDropOffer(daily.OfferJson),
            Upgrades = DailyDropUpgradeDefinitions(settings, dailyUpgradeLevels)
        };
        CaseOpeningFreeCaseAllowanceObj allowance = await _data.GetCaseOpeningFreeCaseAllowance(progress.UserId, cancellationToken);
        result.FreeCaseAllowanceEnabled = settings.FreeCaseAllowanceEnabled;
        result.FreeCaseAllowanceRemaining = allowance.Remaining;
        result.FreeCaseAllowanceQuantity = allowance.Quantity;
        result.FreeCaseAllowanceRefreshUtc = allowance.RefreshUtc;
        result.CaseSaleMultipliers = caseSettings.Values.ToDictionary(
            item => item.CaseKey,
            item => GetCaseSaleMultiplier(item.UnlockCostStars),
            StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static CaseOpeningProgressObj CreateProgress(
        CaseOpeningProgressDbModel progress,
        CaseOpeningGameSettingsObj settings,
        List<string>? unlockedCaseKeys = null)
    {
        return new CaseOpeningProgressObj
        {
            UserId = progress.UserId,
            Stars = progress.Stars,
            GbpPence = progress.GbpPence,
            EconomyMode = settings.EconomyMode,
            CurrencyCode = settings.EconomyMode == CaseOpeningEconomyModes.Gbp ? "GBP" : "STAR",
            ActiveBalanceMinor = settings.EconomyMode == CaseOpeningEconomyModes.Gbp ? progress.GbpPence : progress.Stars,
            SkinSaleRateBasisPoints = settings.SkinSaleRateBasisPoints,
            Xp = progress.Xp,
            Level = CaseOpeningXpLevels.GetLevel(progress.Xp),
            XpIntoLevel = CaseOpeningXpLevels.GetXpIntoLevel(progress.Xp),
            XpForNextLevel = CaseOpeningXpLevels.GetXpForNextLevel(progress.Xp),
            SkipAnimationUnlocked = progress.SkipAnimationUnlocked,
            MultiOpenLevel = progress.MultiOpenLevel,
            OpenSpeedLevel = progress.OpenSpeedLevel,
            SkipAnimationCost = IsGbp(settings) ? settings.SkipAnimationCostGbpPence : settings.SkipAnimationCostStars,
            SkipAnimationXpRequirement = settings.SkipAnimationXpRequirement,
            MultiOpenCost = IsGbp(settings) ? settings.MultiOpenCostGbpPence : settings.MultiOpenCostStars,
            MultiOpenXpRequirement = settings.MultiOpenXpRequirement,
            MaximumMultiOpenLevel = settings.MaximumMultiOpenLevel,
            // Level 0 = 1x, then +.5x per level up to 3x at level 4. Level 5 (the final tier) grants
            // Skip Animation instead of a further multiplier - the reel doesn't get faster than 3x,
            // it gets bypassed entirely.
            OpenSpeedMultiplier = 1m + (Math.Min(progress.OpenSpeedLevel, 4) * .5m),
            OpenSpeedUpgradeCost = IsGbp(settings)
                ? settings.OpenSpeedUpgradeBaseCostGbpPence + (progress.OpenSpeedLevel * settings.OpenSpeedUpgradeCostIncrementGbpPence)
                : settings.OpenSpeedUpgradeBaseCostStars + (progress.OpenSpeedLevel * settings.OpenSpeedUpgradeCostIncrementStars),
            OpenSpeedUpgradeXpRequirement = settings.OpenSpeedUpgradeXpRequirement + progress.OpenSpeedLevel,
            MaximumOpenSpeedLevel = settings.MaximumOpenSpeedLevel,
            MaximumOpenQuantity = settings.MaximumOpenQuantity,
            StorageContainerBaseCostStars = settings.StorageContainerBaseCostStars,
            StorageContainerCostIncrementStars = settings.StorageContainerCostIncrementStars,
            StorageContainerBaseCost = IsGbp(settings) ? settings.StorageContainerBaseCostGbpPence : settings.StorageContainerBaseCostStars,
            StorageContainerCostIncrement = IsGbp(settings) ? settings.StorageContainerCostIncrementGbpPence : settings.StorageContainerCostIncrementStars,
            StorageContainerSlots = settings.StorageContainerSlots,
            MaximumStorageContainers = settings.MaximumStorageContainers,
            SaleValues = new Dictionary<string, int>(SaleValues, StringComparer.OrdinalIgnoreCase),
            UnlockedCaseKeys = unlockedCaseKeys ?? []
        };
    }

    private static int GetSaleValue(string rarityKey)
    {
        return SaleValues.TryGetValue(rarityKey, out int value) ? value : 0;
    }

    private static int GetCollectionRarityOrder(string rarityKey)
    {
        return rarityKey.ToLowerInvariant() switch
        {
            "mil-spec" or "high-grade" => 1,
            "restricted" or "remarkable" => 2,
            "classified" or "exotic" => 3,
            "covert" => 4,
            "rare-special" => 5,
            _ => 99
        };
    }

    private static int GetCaseSaleMultiplier(int unlockCostStars)
    {
        return unlockCostStars switch
        {
            >= 1_000 => 6,
            >= 200 => 4,
            >= 60 => 3,
            >= 20 => 2,
            _ => 1
        };
    }

    private static CaseOpeningBotProgressObj CreateBotProgress(
        CaseOpeningProgressDbModel progress,
        List<CaseOpeningBotServerDbModel> servers,
        List<CaseOpeningBotDbModel> bots,
        CaseOpeningGameSettingsObj settings)
    {
        List<CaseOpeningBotServerObj> serverObjs = servers
            .OrderBy(server => server.CreatedUtc)
            .Select(server =>
            {
                CaseOpeningBotServerObj result = server.Adapt<CaseOpeningBotServerObj>();
                result.Bots = bots
                    .Where(bot => bot.ServerId == server.ServerId)
                    .OrderBy(bot => bot.CreatedUtc)
                    .ToList();
                int serverLevel = result.Bots.Sum(bot => Math.Min(bot.SpeedLevel, MaximumIndividualBotSpeedLevel));
                result.SpeedLevel = serverLevel;
                foreach (CaseOpeningBotDbModel bot in result.Bots)
                {
                    bot.SpeedMultiplier = .5m + (Math.Min(bot.SpeedLevel, MaximumIndividualBotSpeedLevel) * .1m);
                    bot.OpeningIntervalSeconds = Math.Max(1, (int)Math.Ceiling(settings.BotOpeningIntervalSeconds * (.5m / bot.SpeedMultiplier)));
                    bot.NextSpeedUpgradeCost = StarsFromPence(settings.BotSpeedUpgradeBaseCostGbpPence + (serverLevel * settings.BotSpeedUpgradeCostIncrementGbpPence));
                    bot.ActiveNextSpeedUpgradeCost = IsGbp(settings) ? settings.BotSpeedUpgradeBaseCostGbpPence + (serverLevel * settings.BotSpeedUpgradeCostIncrementGbpPence) : bot.NextSpeedUpgradeCost;
                    bot.MaximumSpeedReached = bot.SpeedLevel >= MaximumIndividualBotSpeedLevel;
                }
                result.SpeedMultiplier = result.Bots.Count == 0 ? .5m : result.Bots.Average(bot => bot.SpeedMultiplier);
                result.OpeningIntervalSeconds = result.Bots.Count == 0 ? settings.BotOpeningIntervalSeconds : result.Bots.Min(bot => bot.OpeningIntervalSeconds);
                result.NextSpeedUpgradeCost = result.Bots.Where(bot => !bot.MaximumSpeedReached).Select(bot => bot.NextSpeedUpgradeCost).DefaultIfEmpty(0).Min();
                result.ActiveNextSpeedUpgradeCost = result.Bots.Where(bot => !bot.MaximumSpeedReached).Select(bot => bot.ActiveNextSpeedUpgradeCost).DefaultIfEmpty(0).Min();
                result.MaximumSpeedReached = result.Bots.Count >= BotServerCapacity && result.Bots.All(bot => bot.MaximumSpeedReached);
                return result;
            })
            .ToList();

        return new CaseOpeningBotProgressObj
        {
            Stars = progress.Stars,
            GbpPence = progress.GbpPence,
            EconomyMode = settings.EconomyMode,
            ActiveBalanceMinor = IsGbp(settings) ? progress.GbpPence : progress.Stars,
            ServerCapacity = BotServerCapacity,
            OpeningIntervalSeconds = settings.BotOpeningIntervalSeconds,
            NextServerCost = GetNextBotServerCost(serverObjs.Count, settings),
            NextBotCost = GetNextBotCost(bots.Count, settings),
            ActiveNextServerCost = IsGbp(settings) ? settings.BotServerBaseCostGbpPence + (serverObjs.Count * settings.BotServerCostIncrementGbpPence) : GetNextBotServerCost(serverObjs.Count, settings),
            ActiveNextBotCost = IsGbp(settings) ? (long)Math.Ceiling((double)settings.BotBaseCostGbpPence * Math.Pow((double)settings.BotCostGrowthRate, bots.Count)) : GetNextBotCost(bots.Count, settings),
            MaximumSpeedLevel = MaximumIndividualBotSpeedLevel,
            Servers = serverObjs
        };
    }

    private static int GetNextBotServerCost(int ownedServerCount, CaseOpeningGameSettingsObj settings)
    {
        return settings.BotServerBaseCostStars + (ownedServerCount * settings.BotServerCostIncrementStars);
    }

    private static int GetNextBotCost(int ownedBotCount, CaseOpeningGameSettingsObj settings)
    {
        return (int)Math.Ceiling((double)settings.BotBaseCostStars * Math.Pow((double)settings.BotCostGrowthRate, ownedBotCount));
    }

    public async Task<CaseOpeningStatisticsObj> GetCaseOpeningStatistics(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        ValidateCaseKey(caseKey);
        CaseOpeningCaseObj caseData = await _referenceData.GetCase(caseKey, cancellationToken);
        CaseOpeningOddsObj target = caseData.Odds[^1];
        CaseOpeningStatisticsObj statistics = (await _data.GetCaseOpeningStatistics(
            userId,
            caseKey,
            target.RarityKey,
            cancellationToken)).Adapt<CaseOpeningStatisticsObj>();

        statistics.CaseKey = caseKey;
        statistics.CaseName = caseData.Name;
        statistics.TargetRarityName = target.RarityName;
        statistics.TargetOddsPercentage = target.Percentage;
        statistics.ExpectedOpeningInterval = Math.Max(1, (int)Math.Round(100m / target.Percentage));

        double missChance = 1d - ((double)target.Percentage / 100d);
        statistics.NoTargetStreakProbability = decimal.Round(
            (decimal)(Math.Pow(missChance, statistics.CurrentDryStreak) * 100d),
            2);

        return statistics;
    }

    private static string SelectRarity(List<CaseOpeningOddsObj> odds)
    {
        List<(CaseOpeningOddsObj Odd, int Weight)> weighted = odds
            .Select(odd => (odd, Math.Max(0, (int)(odd.Percentage * 10_000m))))
            .Where(item => item.Item2 > 0)
            .ToList();
        if (weighted.Count == 0) return odds[^1].RarityKey;

        int roll = RandomNumberGenerator.GetInt32(weighted.Sum(item => item.Weight));
        int boundary = 0;
        foreach ((CaseOpeningOddsObj odd, int weight) in weighted)
        {
            boundary += weight;
            if (roll < boundary) return odd.RarityKey;
        }
        return weighted[^1].Odd.RarityKey;
    }

    private static CaseOpeningItemObj SelectReelItem(CaseOpeningCaseObj caseData)
    {
        string rarityKey = SelectRarity(caseData.Odds);
        List<CaseOpeningItemObj> eligible = caseData.Items
            .Where(item => item.RarityKey == rarityKey)
            .ToList();

        // A changing upstream catalogue should not make an otherwise valid opening fail merely
        // because one decorative reel slot has no item for a published rarity.
        if (eligible.Count == 0)
        {
            eligible = caseData.Items;
        }

        return Clone(eligible[RandomNumberGenerator.GetInt32(eligible.Count)]);
    }

    private async Task ApplyUniqueCondition(
        Guid userId,
        CaseOpeningItemObj item,
        string caseType,
        CancellationToken cancellationToken)
    {
        if (caseType == "Sticker Capsule")
        {
            item.MarketHashName = $"Sticker | {item.Name}";
            return;
        }

        decimal minimum = item.MinFloat ?? 0m;
        decimal maximum = item.MaxFloat ?? 1m;
        if (maximum < minimum) maximum = minimum;

        const int maximumAttempts = 12;
        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            // Six decimal places match the useful precision shown by inventory inspection tools.
            // Each candidate uses fresh cryptographic randomness and is checked against this user's
            // previous pulls of the same skin before it becomes the saved result.
            decimal unit = RandomNumberGenerator.GetInt32(1_000_001) / 1_000_000m;
            decimal floatValue = decimal.Round(minimum + ((maximum - minimum) * unit), 6);
            int patternSeed = RandomNumberGenerator.GetInt32(1_001);

            bool conditionExists = await _data.CaseOpeningConditionExists(
                userId,
                item.SourceItemId,
                floatValue,
                patternSeed,
                cancellationToken);

            if (conditionExists)
            {
                continue;
            }

            item.FloatValue = floatValue;
            item.PatternSeed = patternSeed;
            break;
        }

        if (item.FloatValue is null || item.PatternSeed is null)
        {
            throw new InvalidOperationException("A unique condition could not be generated for this opening. Please try again.");
        }

        item.Wear = WearFromFloat(item.FloatValue.Value);
        item.IsStatTrak = item.SupportsStatTrak && RandomNumberGenerator.GetInt32(100) < 10;
        bool souvenir = caseType.Equals("Souvenir Package", StringComparison.OrdinalIgnoreCase)
            || caseType.Equals("Souvenir", StringComparison.OrdinalIgnoreCase);
        item.IsStatTrak = !souvenir && item.IsStatTrak;
        string prefix = souvenir
            ? "Souvenir "
            : $"{(item.IsRareSpecial ? "★ " : string.Empty)}{(item.IsStatTrak ? "StatTrak™ " : string.Empty)}";
        item.MarketHashName = $"{prefix}{item.Name} ({item.Wear})";
    }

    /// <summary>
    /// CS trade-up output float is derived from the average input float and the output skin's
    /// available range. Unlike a case opening, no random output float is rolled here.
    /// </summary>
    private async Task ApplyTradeUpCondition(
        Guid userId,
        CaseOpeningItemObj item,
        decimal averageInputFloat,
        bool isStatTrak,
        CancellationToken cancellationToken)
    {
        decimal minimum = item.MinFloat ?? 0m;
        decimal maximum = item.MaxFloat ?? 1m;
        if (maximum < minimum)
        {
            maximum = minimum;
        }

        decimal normalisedAverage = decimal.Clamp(averageInputFloat, 0m, 1m);
        decimal outputFloat = decimal.Round(minimum + ((maximum - minimum) * normalisedAverage), 6);
        const int maximumAttempts = 12;

        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            int patternSeed = RandomNumberGenerator.GetInt32(1_001);
            bool conditionExists = await _data.CaseOpeningConditionExists(
                userId,
                item.SourceItemId,
                outputFloat,
                patternSeed,
                cancellationToken);

            if (conditionExists)
            {
                continue;
            }

            item.FloatValue = outputFloat;
            item.PatternSeed = patternSeed;
            break;
        }

        if (item.FloatValue is null || item.PatternSeed is null)
        {
            throw new InvalidOperationException("A unique condition could not be generated for this Trade Up Contract. Please try again.");
        }

        item.Wear = WearFromFloat(item.FloatValue.Value);
        item.IsStatTrak = isStatTrak;
        string statTrak = item.IsStatTrak ? "StatTrak™ " : string.Empty;
        item.MarketHashName = $"{statTrak}{item.Name} ({item.Wear})";
    }

    private static string WearFromFloat(decimal value)
    {
        if (value < .07m) return "Factory New";
        if (value < .15m) return "Minimal Wear";
        if (value < .38m) return "Field-Tested";
        if (value < .45m) return "Well-Worn";
        return "Battle-Scarred";
    }

    private async Task<List<CaseOpeningDailyDropRewardObj>> CreateDailyDropOffer(CaseOpeningGameSettingsObj settings, List<string> unlockedCaseKeys, IReadOnlyDictionary<string, int> levels, CancellationToken cancellationToken)
    {
        List<CaseOpeningCaseObj> unlocked = [];
        foreach (string key in unlockedCaseKeys.Distinct(StringComparer.OrdinalIgnoreCase)) unlocked.Add(await _referenceData.GetCase(key, cancellationToken));
        List<CaseOpeningCaseObj> paid = unlocked.Where(item => IsGbp(settings) ? item.PurchaseCostGbpPence > 0 : item.PurchaseCostStars > 0).OrderBy(item => item.Tier).ToList();
        CaseOpeningCaseObj fallback = unlocked.FirstOrDefault() ?? await _referenceData.GetCase(StarterCaseKey, cancellationToken);
        int qualityLevel = levels.GetValueOrDefault("quality");
        List<CaseOpeningCaseObj> qualityPool = paid.Skip(Math.Min(qualityLevel, Math.Max(0, paid.Count - 1))).ToList();
        CaseOpeningCaseObj caseReward = qualityPool.Count == 0 ? fallback : qualityPool[RandomNumberGenerator.GetInt32(qualityPool.Count)];
        CaseOpeningCaseObj stickerReward = unlocked.FirstOrDefault(item => item.Type.Contains("Sticker", StringComparison.OrdinalIgnoreCase)) ?? caseReward;
        List<(CaseOpeningCaseObj Case, CaseOpeningItemObj Item)> skins = [];
        foreach (CaseOpeningCaseObj @case in unlocked)
            foreach (CaseOpeningItemObj item in @case.Items.Where(item => item.RarityKey is "mil-spec" or "restricted" or "classified"))
            {
                item.EstimatedPrice = await _prices.GetEstimatedPrice(item.MarketHashName, cancellationToken);
                if (item.EstimatedPrice is > 0 and < 50m) skins.Add((@case, item));
            }
        (CaseOpeningCaseObj Case, CaseOpeningItemObj Item)? skinReward = skins.Count == 0 ? null : skins[RandomNumberGenerator.GetInt32(skins.Count)];
        List<CaseOpeningDailyDropRewardObj> result = [
            new() { RewardKey="money", Kind="money", Name="Cash reward", Description="Add currency to your balance.", AmountMinor=100 + (levels.GetValueOrDefault("cash") * 50) },
            new() { RewardKey="cases", Kind="cases", Name=$"{10 + (levels.GetValueOrDefault("case-stash") * 5)} × {caseReward.Name}", Description="A paid unlocked case pack.", CaseKey=caseReward.CaseKey, AmountMinor=10 + (levels.GetValueOrDefault("case-stash") * 5), ImageUrl=caseReward.ImageUrl },
            new() { RewardKey="sticker", Kind="cases", Name=$"5 × {stickerReward.Name}", Description="Sticker capsule reward.", CaseKey=stickerReward.CaseKey, AmountMinor=5, ImageUrl=stickerReward.ImageUrl }
        ];
        if (skinReward is not null) result.Add(new() { RewardKey="skin", Kind="skin", Name=skinReward.Value.Item.Name, Description="Blue, purple or pink skin under £50.", CaseKey=skinReward.Value.Case.CaseKey, Item=skinReward.Value.Item, ImageUrl=skinReward.Value.Item.ImageUrl });
        else result.Add(new() { RewardKey="bonus-cases", Kind="cases", Name=$"5 × {caseReward.Name}", Description="Bonus case pack while no eligible skin is priced below £50.", CaseKey=caseReward.CaseKey, AmountMinor=5, ImageUrl=caseReward.ImageUrl });
        return result;
    }

    private static List<CaseOpeningDailyDropRewardObj> ParseDailyDropOffer(string offerJson)
        => string.IsNullOrWhiteSpace(offerJson) ? [] : JsonSerializer.Deserialize<List<CaseOpeningDailyDropRewardObj>>(offerJson) ?? [];

    private static (int Max, int Stars, long Pence) DailyDropUpgradeCost(string key, int level) => key switch
    {
        "focus" => (3, 150 + (level * 100), 150 + (level * 100)),
        "cash" => (3, 200 + (level * 150), 200 + (level * 150)),
        "case-stash" => (3, 250 + (level * 175), 250 + (level * 175)),
        "quality" => (3, 300 + (level * 200), 300 + (level * 200)),
        _ => throw new InvalidOperationException("That Daily Drop upgrade does not exist.")
    };

    private static List<CaseOpeningDailyDropUpgradeObj> DailyDropUpgradeDefinitions(CaseOpeningGameSettingsObj settings, IReadOnlyDictionary<string, int> levels)
    {
        (string Key, string Name, string Description)[] definitions = [
            ("focus", "Focused collector", "Reduce the XP needed for each Daily Drop by 10."),
            ("cash", "Cash cache", "Increase the currency reward."),
            ("case-stash", "Case stash", "Add 5 cases to the case-pack reward."),
            ("quality", "Higher stakes", "Bias case packs toward higher unlocked tiers.")];
        return definitions.Select(definition =>
        {
            int level = levels.GetValueOrDefault(definition.Key);
            (int max, int stars, long pence) = DailyDropUpgradeCost(definition.Key, level);
            return new CaseOpeningDailyDropUpgradeObj { UpgradeKey=definition.Key, Name=definition.Name, Description=definition.Description, Level=level, MaximumLevel=max, Cost=IsGbp(settings) ? pence : stars };
        }).ToList();
    }

    private static bool IsGbp(CaseOpeningGameSettingsObj settings) =>
        string.Equals(settings.EconomyMode, CaseOpeningEconomyModes.Gbp, StringComparison.OrdinalIgnoreCase);

    private static void ApplyActiveCaseCosts(CaseOpeningCaseObj item, string economyMode)
    {
        bool gbp = string.Equals(economyMode, CaseOpeningEconomyModes.Gbp, StringComparison.OrdinalIgnoreCase);
        item.UnlockCost = gbp ? item.UnlockCostGbpPence : item.UnlockCostStars;
        item.PurchaseCost = gbp ? item.PurchaseCostGbpPence : item.PurchaseCostStars;
    }

    private sealed record MarketVariant(string Name, decimal Weight);

    private static List<MarketVariant> MarketVariants(CaseOpeningItemObj item, string caseType)
    {
        if (caseType.Equals("Sticker Capsule", StringComparison.OrdinalIgnoreCase))
        {
            return [new MarketVariant($"Sticker | {item.Name}", 1m)];
        }

        decimal minimum = item.MinFloat ?? 0m;
        decimal maximum = item.MaxFloat ?? 1m;
        if (maximum < minimum) maximum = minimum;
        (string Wear, decimal Start, decimal End)[] bands =
        [
            ("Factory New", 0m, .07m),
            ("Minimal Wear", .07m, .15m),
            ("Field-Tested", .15m, .38m),
            ("Well-Worn", .38m, .45m),
            ("Battle-Scarred", .45m, 1.000001m)
        ];
        List<(string Wear, decimal Weight)> wears = [];
        if (maximum == minimum)
        {
            wears.Add((WearFromFloat(minimum), 1m));
        }
        else
        {
            foreach ((string wear, decimal start, decimal end) in bands)
            {
                decimal overlap = Math.Max(0m, Math.Min(maximum, end) - Math.Max(minimum, start));
                if (overlap > 0) wears.Add((wear, overlap / (maximum - minimum)));
            }
        }

        bool souvenir = caseType.Equals("Souvenir Package", StringComparison.OrdinalIgnoreCase)
            || caseType.Equals("Souvenir", StringComparison.OrdinalIgnoreCase);
        string normalPrefix = souvenir ? "Souvenir " : item.IsRareSpecial ? "★ " : string.Empty;
        string statTrakPrefix = item.IsRareSpecial ? "★ StatTrak™ " : "StatTrak™ ";
        List<MarketVariant> variants = [];
        foreach ((string wear, decimal weight) in wears)
        {
            variants.Add(new MarketVariant($"{normalPrefix}{item.Name} ({wear})", item.SupportsStatTrak && !souvenir ? weight * .9m : weight));
            if (item.SupportsStatTrak && !souvenir)
            {
                variants.Add(new MarketVariant($"{statTrakPrefix}{item.Name} ({wear})", weight * .1m));
            }
        }
        return variants;
    }

    private static CaseOpeningCaseMarketValueObj BuildCaseMarketValue(CaseOpeningCaseObj caseData, IReadOnlyDictionary<string, decimal> prices)
    {
        decimal expectedValue = 0m;
        int totalVariants = 0;
        int pricedVariants = 0;
        bool complete = true;
        foreach (CaseOpeningOddsObj odds in caseData.Odds)
        {
            List<CaseOpeningItemObj> rarityItems = caseData.Items.Where(item => item.RarityKey == odds.RarityKey).ToList();
            if (rarityItems.Count == 0) continue;
            decimal rarityValue = 0m;
            foreach (CaseOpeningItemObj item in rarityItems)
            {
                List<MarketVariant> variants = MarketVariants(item, caseData.Type);
                totalVariants += variants.Count;
                decimal itemValue = 0m;
                foreach (MarketVariant variant in variants)
                {
                    if (!prices.TryGetValue(variant.Name, out decimal price))
                    {
                        complete = false;
                        continue;
                    }
                    pricedVariants++;
                    itemValue += price * variant.Weight;
                }
                rarityValue += itemValue / rarityItems.Count;
            }
            expectedValue += rarityValue * (odds.Percentage / 100m);
        }

        decimal? openingCost = prices.TryGetValue(caseData.Name, out decimal cost) ? cost : null;
        decimal? resolvedExpectedValue = complete && totalVariants > 0 ? decimal.Round(expectedValue, 2) : null;
        decimal? profit = openingCost is not null && resolvedExpectedValue is not null
            ? decimal.Round(resolvedExpectedValue.Value - openingCost.Value, 2)
            : null;
        return new CaseOpeningCaseMarketValueObj
        {
            CaseKey = caseData.CaseKey,
            CaseName = caseData.Name,
            OpeningCost = openingCost,
            ExpectedValue = resolvedExpectedValue,
            ExpectedProfit = profit,
            ReturnPercentage = openingCost is > 0 && resolvedExpectedValue is not null
                ? decimal.Round((resolvedExpectedValue.Value / openingCost.Value) * 100m, 1)
                : null,
            PricedVariants = pricedVariants,
            TotalVariants = totalVariants
        };
    }

    private static CaseOpeningItemObj Clone(CaseOpeningItemObj item)
    {
        return item.Adapt<CaseOpeningItemObj>();
    }

    private static void ValidateGameSettings(CaseOpeningGameSettingsObj settings)
    {
        if (settings.XpPerCaseOpen < 0 || settings.SkipAnimationCostStars < 0 || settings.MultiOpenCostStars < 0
            || settings.SkipAnimationXpRequirement < 0 || settings.MultiOpenXpRequirement < 0
            || settings.OpenSpeedUpgradeBaseCostStars < 0 || settings.OpenSpeedUpgradeCostIncrementStars < 0
            || settings.OpenSpeedUpgradeXpRequirement < 0
            || settings.BotServerBaseCostStars < 0 || settings.BotServerCostIncrementStars < 0
            || settings.BotBaseCostStars < 0 || settings.StorageContainerBaseCostStars < 0
            || settings.StorageContainerCostIncrementStars < 0 || settings.StorageContainerSlots < 1
            || settings.TradeUpRecipeCostStars < 0 || settings.TradeUpRecipeCostGbpPence < 0
            || settings.TradeUpSlotUpgradeBaseCostStars < 0 || settings.TradeUpSlotUpgradeCostIncrementStars < 0
            || settings.TradeUpSlotUpgradeBaseCostGbpPence < 0 || settings.TradeUpSlotUpgradeCostIncrementGbpPence < 0
            || settings.TradeUpHoldingUpgradeBaseCostStars < 0 || settings.TradeUpHoldingUpgradeCostIncrementStars < 0
            || settings.TradeUpHoldingUpgradeBaseCostGbpPence < 0 || settings.TradeUpHoldingUpgradeCostIncrementGbpPence < 0)
        {
            throw new InvalidOperationException("Costs and XP requirements cannot be negative.");
        }

        if (settings.MaximumMultiOpenLevel < 1 || settings.MaximumOpenSpeedLevel < 1
            || settings.MaximumOpenQuantity < 1 || settings.MaximumStorageContainers < 0)
        {
            throw new InvalidOperationException("Maximum multi-open level, speed level, and open quantity must be at least 1, and storage limits cannot be negative.");
        }

        if (settings.BotOpeningIntervalSeconds < 1)
        {
            throw new InvalidOperationException("Bot opening interval must be at least 1 second.");
        }

        if (settings.BotCostGrowthRate < 1m)
        {
            throw new InvalidOperationException("Bot cost growth rate must be at least 1.0.");
        }
    }

    private static int StarsFromPence(long pence) => pence <= 0 ? 0 : CaseOpeningBalancePolicy.PenceToStars(pence);

    private static void NormalizeStarDisplaysFromGbp(CaseOpeningGameSettingsObj settings)
    {
        settings.SkipAnimationCostStars = StarsFromPence(settings.SkipAnimationCostGbpPence);
        settings.MultiOpenCostStars = StarsFromPence(settings.MultiOpenCostGbpPence);
        settings.OpenSpeedUpgradeBaseCostStars = StarsFromPence(settings.OpenSpeedUpgradeBaseCostGbpPence);
        settings.OpenSpeedUpgradeCostIncrementStars = StarsFromPence(settings.OpenSpeedUpgradeCostIncrementGbpPence);
        settings.BotServerBaseCostStars = StarsFromPence(settings.BotServerBaseCostGbpPence);
        settings.BotServerCostIncrementStars = StarsFromPence(settings.BotServerCostIncrementGbpPence);
        settings.BotBaseCostStars = StarsFromPence(settings.BotBaseCostGbpPence);
        settings.StorageContainerBaseCostStars = StarsFromPence(settings.StorageContainerBaseCostGbpPence);
        settings.StorageContainerCostIncrementStars = StarsFromPence(settings.StorageContainerCostIncrementGbpPence);
        settings.TradeUpRecipeCostStars = StarsFromPence(settings.TradeUpRecipeCostGbpPence);
        settings.TradeUpSlotUpgradeBaseCostStars = StarsFromPence(settings.TradeUpSlotUpgradeBaseCostGbpPence);
        settings.TradeUpSlotUpgradeCostIncrementStars = StarsFromPence(settings.TradeUpSlotUpgradeCostIncrementGbpPence);
        settings.TradeUpHoldingUpgradeBaseCostStars = StarsFromPence(settings.TradeUpHoldingUpgradeBaseCostGbpPence);
        settings.TradeUpHoldingUpgradeCostIncrementStars = StarsFromPence(settings.TradeUpHoldingUpgradeCostIncrementGbpPence);
    }

    private static void ValidateCaseKey(string caseKey)
    {
        if (string.IsNullOrWhiteSpace(caseKey) || caseKey.Length > 80)
        {
            throw new InvalidOperationException("That case is not available.");
        }
    }
}
