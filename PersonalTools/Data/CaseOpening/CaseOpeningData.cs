using MySqlConnector;
using System.Text.Json;
using PersonalTools.Entities.CaseOpening;

namespace PersonalTools.Data.CaseOpening;

public interface ICaseOpeningData
{
    Task<List<CaseOpeningHistoryDbModel>> GetCaseOpeningHistory(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCollectionDbModel>> GetCaseOpeningCollection(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCollectionDbModel>> GetCaseOpeningCollections(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel> GetCaseOpeningProgress(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningOwnedCaseDbModel>> GetCaseOpeningOwnedCases(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryCapacityDbModel> GetCaseOpeningInventoryCapacity(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningPlayerStatsDbModel> GetCaseOpeningPlayerStats(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningAchievementDbModel>> GetCaseOpeningAchievements(Guid userId, CancellationToken cancellationToken = default);
    Task EvaluateCaseOpeningAchievements(Guid userId, CancellationToken cancellationToken = default);
    Task RecordCaseOpeningPlayerActivity(Guid userId, int casesOpened, int skinsObtained, int tradeUpsCompleted, int unlocksEarned, string rarityKey, bool isStatTrak, int casesPurchased, int casePurchaseStarsSpent, int saleStarsEarned, int pullValueStars, int starsSpent, int levelRewardStars, int upgradesPurchased, CancellationToken cancellationToken = default);
    Task RecordCaseOpeningLogin(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ClaimCaseOpeningLevelReward(Guid userId, int level, int starsAwarded, long gbpPenceAwarded, CancellationToken cancellationToken = default);
    Task<bool> RecordCompletedCaseOpeningCollection(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task<bool> RecordCompletedCaseOpeningRarity(Guid userId, string caseKey, string rarityKey, CancellationToken cancellationToken = default);
    Task<List<string>> GetCaseOpeningUnlockedCases(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel?> UnlockCaseOpeningCase(Guid userId, string caseKey, int costStars, long costGbpPence, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel?> UnlockCaseOpeningUpgrade(Guid userId, string upgradeKey, int costStars, long costGbpPence, int maximumMultiOpenLevel, int maximumOpenSpeedLevel, CancellationToken cancellationToken = default);
    Task<CaseOpeningSellResultDbModel?> SellCaseOpeningInventory(Guid userId, List<Guid> openingIds, int starsAwarded, long gbpPenceAwarded, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryLockObj?> SetCaseOpeningInventoryLock(Guid userId, Guid openingId, bool isLocked, CancellationToken cancellationToken = default);
    Task<CaseOpeningInventoryUpgradeDbModel> GetCaseOpeningInventoryUpgrades(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningUpgradeDefinitionObj>> GetCaseOpeningUpgradeDefinitions(Guid userId, CancellationToken cancellationToken = default);
    Task UnlockCaseOpeningInventoryUpgrade(Guid userId, string upgradeKey, int costStars, long costGbpPence, CancellationToken cancellationToken = default);
    Task<CaseOpeningUserPreferencesObj> GetCaseOpeningUserPreferences(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningUserPreferencesObj> SetCaseOpeningLastQuantity(Guid userId, int quantity, CancellationToken cancellationToken = default);
    Task<CaseOpeningUserPreferencesObj> SetCaseOpeningAutomationPreferences(Guid userId, CaseOpeningUserPreferencesObj preferences, CancellationToken cancellationToken = default);
    Task<CaseOpeningUserPreferencesObj> SetCaseOpeningSocialPreferences(Guid userId, CaseOpeningUserPreferencesObj preferences, CancellationToken cancellationToken = default);
    Task<List<string>> GetCaseBattleReactionUnlocks(Guid userId, CancellationToken cancellationToken = default);
    Task PurchaseCaseBattleReaction(Guid userId, string reactionKey, int costStars, long costGbpPence, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningAutoBuyRuleDbModel>> GetCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default);
    Task SetCaseOpeningAutoBuyRule(Guid userId, string caseKey, int thresholdQuantity, int purchaseQuantity, bool isEnabled, int ruleSlotCap, CancellationToken cancellationToken = default);
    Task DeleteCaseOpeningAutoBuyRule(Guid userId, string caseKey, CancellationToken cancellationToken = default);
    Task SetCaseOpeningAutoSellPreference(Guid userId, string rarityKey, bool enabled, bool preserveStatTrak, CancellationToken cancellationToken = default);
    Task ExecuteCaseOpeningTradeUp(
        Guid userId,
        CaseOpeningTradeUpDbModel tradeUp,
        List<Guid> openingIds,
        CaseOpeningHistoryDbModel output,
        Guid? recipeId,
        bool? isMatch,
        CancellationToken cancellationToken = default);
    Task<List<CaseOpeningTradeUpRecipeObj>> GetCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningTradeUpHistoryObj>> GetCaseOpeningTradeUpHistory(Guid userId, CancellationToken cancellationToken = default);
    Task CreateCaseOpeningTradeUpRecipe(Guid userId, CaseOpeningTradeUpRecipeDbModel recipe, int costStars, long costGbpPence, int recipeSlotCap, CancellationToken cancellationToken = default);
    Task SetCaseOpeningTradeUpRecipeActive(Guid userId, Guid recipeId, bool isActive, int recipeSlotCap, CancellationToken cancellationToken = default);
    Task DeleteCaseOpeningTradeUpRecipe(Guid userId, Guid recipeId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningTradeUpHoldingObj>> GetCaseOpeningTradeUpHoldings(Guid userId, CancellationToken cancellationToken = default);
    Task CollectCaseOpeningTradeUpHolding(Guid userId, Guid holdingId, CancellationToken cancellationToken = default);
    Task UpgradeCaseOpeningTradeUpRecipeSlots(Guid userId, int costStars, long costGbpPence, int maximumSlots, CancellationToken cancellationToken = default);
    Task UpgradeCaseOpeningTradeUpRecipeHolding(Guid userId, Guid recipeId, int costStars, long costGbpPence, int maximumCapacity, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningBotServerDbModel>> GetCaseOpeningBotServers(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningBotDbModel>> GetCaseOpeningBots(Guid userId, CancellationToken cancellationToken = default);
    Task PurchaseCaseOpeningBotServer(Guid userId, Guid serverId, int costStars, long costGbpPence, CancellationToken cancellationToken = default);
    Task PurchaseCaseOpeningBot(Guid userId, Guid serverId, Guid botId, int costStars, long costGbpPence, CancellationToken cancellationToken = default);
    Task UpgradeCaseOpeningBot(Guid userId, Guid botId, int costStars, long costGbpPence, int maximumLevel, CancellationToken cancellationToken = default);
    Task SetCaseOpeningBotServerEnabled(Guid userId, Guid serverId, bool isEnabled, CancellationToken cancellationToken = default);
    Task<bool> ClaimCaseOpeningBotCycle(Guid userId, Guid botId, CancellationToken cancellationToken = default);
    Task<bool> CaseOpeningConditionExists(Guid userId, string sourceItemId, decimal floatValue, int patternSeed, CancellationToken cancellationToken = default);
    Task<bool> CaseOpeningCollectionItemExists(Guid userId, string caseKey, string sourceItemId, CancellationToken cancellationToken = default);
    Task SaveCaseOpening(Guid userId, CaseOpeningHistoryDbModel opening, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSpecialVariantRuleDbModel>> GetActiveCaseOpeningSpecialVariantRules(CancellationToken cancellationToken = default);
    Task SaveCaseOpeningSpecialVariant(Guid openingId, CaseOpeningSpecialVariantRuleDbModel variant, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSpecialVariantRuleDbModel>> GetCaseOpeningSpecialVariantRules(CancellationToken cancellationToken = default);
    Task SaveCaseOpeningSpecialVariantRule(Guid ruleId, CaseOpeningSpecialVariantRuleRequestObj rule, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSpecialVariantPriceSnapshotObj>> GetCaseOpeningSpecialVariantPriceSnapshots(CancellationToken cancellationToken = default);
    Task CreateCaseOpeningSpecialVariantPriceSnapshot(CaseOpeningSpecialVariantPriceSnapshotObj snapshot, Dictionary<Guid, decimal> prices, CancellationToken cancellationToken = default);
    Task ActivateCaseOpeningSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task DeleteCaseOpeningSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task<CaseOpeningStatisticsDbModel> GetCaseOpeningStatistics(Guid userId, string caseKey, string targetRarityKey, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel?> AddCaseOpeningXp(Guid userId, int xpDelta, CancellationToken cancellationToken = default);
    Task<CaseOpeningDailyDropDbModel> GetCaseOpeningDailyDrop(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningDailyDropDbModel> AddCaseOpeningDailyDropXp(Guid userId, int xpDelta, int requiredXp, CancellationToken cancellationToken = default);
    Task<CaseOpeningDailyDropDbModel> SetCaseOpeningDailyDropOffer(Guid userId, string offerJson, CancellationToken cancellationToken = default);
    Task ClaimCaseOpeningDailyDrop(Guid userId, List<string> rewardKeys, string economyMode, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningDailyDropUpgradeDbModel>> GetCaseOpeningDailyDropUpgrades(Guid userId, CancellationToken cancellationToken = default);
    Task UnlockCaseOpeningDailyDropUpgrade(Guid userId, string upgradeKey, int costStars, long costGbpPence, string economyMode, CancellationToken cancellationToken = default);
    Task<int> GetCaseOpeningDailyDropRequiredXp(CancellationToken cancellationToken = default);
    Task SetCaseOpeningDailyDropRequiredXp(int requiredXp, CancellationToken cancellationToken = default);
    Task ResetCaseOpeningDailyDrop(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningGameSettingsObj> GetGameSettings(CancellationToken cancellationToken = default);
    Task SetGameSettings(CaseOpeningGameSettingsObj settings, CancellationToken cancellationToken = default);
    Task<bool> GetCaseOpeningSkillTreeEnabled(CancellationToken cancellationToken = default);
    Task SetCaseOpeningSkillTreeEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task<bool> GetGuestAccessEnabled(CancellationToken cancellationToken = default);
    Task SetGuestAccessEnabled(bool enabled, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningCaseSettingsObj>> GetCaseSettings(CancellationToken cancellationToken = default);
    Task<List<CaseOpeningTierEconomySettingsObj>> GetTierEconomySettings(CancellationToken cancellationToken = default);
    Task SetTierEconomySettings(int tier, int targetProfitBasisPoints, int priceRoundingPence, CancellationToken cancellationToken = default);
    Task SetCaseSettings(string caseKey, int tier, int unlockCostStars, long unlockCostGbpPence, int purchaseCostStars, long purchaseCostGbpPence, int xpRequirement, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningXpByRarityObj>> GetXpByRarity(CancellationToken cancellationToken = default);
    Task SetXpByRarity(string rarityKey, int xpAwarded, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningUpgradeDefinitionObj>> GetInventoryUpgradeSettings(CancellationToken cancellationToken = default);
    Task SetInventoryUpgradeSettings(string upgradeKey, int costStars, long costGbpPence, int requiredLevel, CancellationToken cancellationToken = default);
    Task<CaseOpeningCasePurchaseResultObj?> PurchaseCaseOpeningCases(Guid userId, string caseKey, int quantity, int purchaseCostStars, long purchaseCostGbpPence, CancellationToken cancellationToken = default);
    Task<CaseOpeningCaseDiscardResultObj?> DiscardCaseOpeningCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default);
    Task<CaseOpeningStoragePurchaseResultObj?> PurchaseCaseOpeningStorageContainer(Guid userId, Guid storageContainerId, int costStars, long costGbpPence, int slots, int maximumContainers, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel?> SetCaseOpeningProgressDev(Guid userId, int stars, long gbpPence, int xp, CancellationToken cancellationToken = default);
    Task<CaseOpeningFreeCaseAllowanceObj> GetCaseOpeningFreeCaseAllowance(Guid userId, CancellationToken cancellationToken = default);
    Task<CaseOpeningProgressDbModel?> SetCaseOpeningUpgradesDev(Guid userId, bool skipAnimationUnlocked, int multiOpenLevel, int openSpeedLevel, CancellationToken cancellationToken = default);
    Task SetCaseOpeningCaseUnlockDev(Guid userId, string caseKey, bool unlock, CancellationToken cancellationToken = default);
    Task<List<string>> GetCaseOpeningDevDropRarityGroups(Guid userId, CancellationToken cancellationToken = default);
    Task SetCaseOpeningDevDropRarityGroups(Guid userId, List<string> rarityGroups, CancellationToken cancellationToken = default);
    Task ResetCaseOpeningProgressDev(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningPriceSnapshotDbModel>> GetCaseOpeningPriceSnapshots(CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSnapshotPriceDbModel>> GetActiveCaseOpeningSnapshotPrices(CancellationToken cancellationToken = default);
    Task<decimal?> GetActiveCaseOpeningSnapshotPrice(string marketHashName, CancellationToken cancellationToken = default);
    Task CreateCaseOpeningPriceSnapshot(CaseOpeningPriceSnapshotDbModel snapshot, List<CaseOpeningSnapshotPriceDbModel> prices, CancellationToken cancellationToken = default);
    Task ActivateCaseOpeningPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task DeleteCaseOpeningPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
}

public sealed class CaseOpeningData : ICaseOpeningData
{
    private readonly IMariaDbDataAccess _database;

    public CaseOpeningData(IMariaDbDataAccess database)
    {
        _database = database;
    }

    public Task<List<CaseOpeningHistoryDbModel>> GetCaseOpeningHistory(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_history_get",
            ReadHistory,
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task<List<CaseOpeningCollectionDbModel>> GetCaseOpeningCollection(
        Guid userId,
        string caseKey,
        CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_collection_get",
            ReadCollection,
            Parameters(("p_user_id", userId), ("p_case_key", caseKey)),
            cancellationToken);
    }

    public Task<List<CaseOpeningPriceSnapshotDbModel>> GetCaseOpeningPriceSnapshots(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_price_snapshots_get", ReadPriceSnapshot, cancellationToken: cancellationToken);
    }

    public Task<List<CaseOpeningSnapshotPriceDbModel>> GetActiveCaseOpeningSnapshotPrices(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_price_snapshot_active_items_get", ReadSnapshotPrice, cancellationToken: cancellationToken);
    }

    public async Task<decimal?> GetActiveCaseOpeningSnapshotPrice(string marketHashName, CancellationToken cancellationToken = default)
    {
        CaseOpeningSnapshotPriceDbModel? result = await _database.GetDataSP(
            "sp_case_opening_price_snapshot_active_price_get",
            ReadSnapshotPrice,
            Parameters(("p_market_hash_name", marketHashName)),
            cancellationToken);
        return result?.Price;
    }

    public Task CreateCaseOpeningPriceSnapshot(CaseOpeningPriceSnapshotDbModel snapshot, List<CaseOpeningSnapshotPriceDbModel> prices, CancellationToken cancellationToken = default)
    {
        string payload = JsonSerializer.Serialize(prices.Select(item => new
        {
            marketHashName = item.MarketHashName,
            price = item.Price,
            minimumPrice = item.MinimumPrice,
            meanPrice = item.MeanPrice,
            medianPrice = item.MedianPrice,
            suggestedPrice = item.SuggestedPrice,
            quantity = item.Quantity,
            sourceUpdatedUtc = item.SourceUpdatedUtc?.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
            isFallback = item.IsFallback,
            priceSource = item.PriceSource,
            priceMethod = item.PriceMethod,
            sourceMarketHashName = item.SourceMarketHashName
        }));
        return _database.ExecuteSP(
            "sp_case_opening_price_snapshot_create",
            Parameters(
                ("p_snapshot_id", snapshot.PriceSnapshotId),
                ("p_name", snapshot.Name),
                ("p_source", snapshot.Source),
                ("p_currency", snapshot.Currency),
                ("p_price_basis", snapshot.PriceBasis),
                ("p_source_item_count", snapshot.SourceItemCount),
                ("p_matched_item_count", snapshot.MatchedItemCount),
                ("p_items", payload)),
            cancellationToken);
    }

    public Task ActivateCaseOpeningPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_price_snapshot_activate",
            Parameters(("p_snapshot_id", snapshotId)),
            cancellationToken);
    }

    public Task DeleteCaseOpeningPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_price_snapshot_delete", Parameters(("p_snapshot_id", snapshotId)), cancellationToken);

    public Task<List<CaseOpeningCollectionDbModel>> GetCaseOpeningCollections(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_collections_get",
            ReadCollection,
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public async Task<CaseOpeningProgressDbModel> GetCaseOpeningProgress(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_progress_get",
            ReadProgress,
            Parameters(("p_user_id", userId)),
            cancellationToken) ?? new CaseOpeningProgressDbModel { UserId = userId };
    }

    public Task<List<CaseOpeningOwnedCaseDbModel>> GetCaseOpeningOwnedCases(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_owned_cases_get",
            reader => new CaseOpeningOwnedCaseDbModel
            {
                CaseKey = reader.GetString("CaseKey"),
                Quantity = reader.GetInt32("Quantity")
            },
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public async Task<CaseOpeningInventoryCapacityDbModel> GetCaseOpeningInventoryCapacity(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_inventory_capacity_get",
            ReadInventoryCapacity,
            Parameters(("p_user_id", userId)),
            cancellationToken) ?? new CaseOpeningInventoryCapacityDbModel
        {
            BaseCapacity = 1000,
            TotalCapacity = 1000,
            AvailableSlots = 1000
        };
    }

    public Task<CaseOpeningProgressDbModel?> UnlockCaseOpeningUpgrade(
        Guid userId,
        string upgradeKey,
        int costStars,
        long costGbpPence,
        int maximumMultiOpenLevel,
        int maximumOpenSpeedLevel,
        CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_upgrade_unlock",
            ReadProgress,
            Parameters(
                ("p_user_id", userId),
                ("p_upgrade_key", upgradeKey),
                ("p_cost_stars", costStars),
                ("p_cost_gbp_pence", costGbpPence),
                ("p_max_multi_open_level", maximumMultiOpenLevel),
                ("p_max_open_speed_level", maximumOpenSpeedLevel)),
            cancellationToken);
    }

    public Task<List<string>> GetCaseOpeningUnlockedCases(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_unlocked_cases_get",
            reader => reader.GetString("CaseKey"),
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task<CaseOpeningProgressDbModel?> UnlockCaseOpeningCase(
        Guid userId,
        string caseKey,
        int costStars,
        long costGbpPence,
        CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_case_unlock",
            ReadProgress,
            Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence)),
            cancellationToken);
    }

    public Task<CaseOpeningSellResultDbModel?> SellCaseOpeningInventory(
        Guid userId,
        List<Guid> openingIds,
        int starsAwarded,
        long gbpPenceAwarded,
        CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_inventory_sell",
            ReadSellResult,
            Parameters(
                ("p_user_id", userId),
                ("p_opening_ids", JsonSerializer.Serialize(openingIds)),
                ("p_item_count", openingIds.Count),
                ("p_stars_awarded", starsAwarded),
                ("p_gbp_pence_awarded", gbpPenceAwarded)),
            cancellationToken);
    }

    public Task<CaseOpeningInventoryLockObj?> SetCaseOpeningInventoryLock(
        Guid userId,
        Guid openingId,
        bool isLocked,
        CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_inventory_lock_set",
            reader => new CaseOpeningInventoryLockObj { OpeningId = openingId, IsLocked = reader.GetBoolean("IsLocked") },
            Parameters(("p_user_id", userId), ("p_opening_id", openingId), ("p_is_locked", isLocked)),
            cancellationToken);
    }

    public async Task<CaseOpeningPlayerStatsDbModel> GetCaseOpeningPlayerStats(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_player_stats_get",
            ReadPlayerStats,
            Parameters(("p_user_id", userId)),
            cancellationToken) ?? new CaseOpeningPlayerStatsDbModel { UserId = userId };
    }

    public Task<List<CaseOpeningAchievementDbModel>> GetCaseOpeningAchievements(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_achievements_get",
            ReadAchievement,
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task EvaluateCaseOpeningAchievements(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_achievements_evaluate",
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task RecordCaseOpeningPlayerActivity(
        Guid userId,
        int casesOpened,
        int skinsObtained,
        int tradeUpsCompleted,
        int unlocksEarned,
        string rarityKey,
        bool isStatTrak,
        int casesPurchased,
        int casePurchaseStarsSpent,
        int saleStarsEarned,
        int pullValueStars,
        int starsSpent,
        int levelRewardStars,
        int upgradesPurchased,
        CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_player_stats_add",
            Parameters(
                ("p_user_id", userId),
                ("p_cases_opened", casesOpened),
                ("p_skins_obtained", skinsObtained),
                ("p_trade_ups_completed", tradeUpsCompleted),
                ("p_unlocks_earned", unlocksEarned),
                ("p_rarity_key", rarityKey),
                ("p_is_stat_trak", isStatTrak),
                ("p_cases_purchased", casesPurchased),
                ("p_case_purchase_stars_spent", casePurchaseStarsSpent),
                ("p_sale_stars_earned", saleStarsEarned),
                ("p_pull_value_stars", pullValueStars),
                ("p_stars_spent", starsSpent),
                ("p_level_reward_stars", levelRewardStars),
                ("p_upgrades_purchased", upgradesPurchased)),
            cancellationToken);
    }

    public Task RecordCaseOpeningLogin(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_login_record",
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public async Task<bool> ClaimCaseOpeningLevelReward(
        Guid userId,
        int level,
        int starsAwarded,
        long gbpPenceAwarded,
        CancellationToken cancellationToken = default)
    {
        int claimed = await _database.GetScalarSP<int>(
            "sp_case_opening_level_reward_claim",
            Parameters(("p_user_id", userId), ("p_level", level), ("p_stars_awarded", starsAwarded), ("p_gbp_pence_awarded", gbpPenceAwarded)),
            cancellationToken);

        return claimed == 1;
    }

    public async Task<bool> RecordCompletedCaseOpeningCollection(Guid userId, string caseKey, CancellationToken cancellationToken = default)
    {
        int recorded = await _database.GetScalarSP<int>(
            "sp_case_opening_collection_completion_record",
            Parameters(("p_user_id", userId), ("p_case_key", caseKey)),
            cancellationToken);

        return recorded == 1;
    }

    public async Task<bool> RecordCompletedCaseOpeningRarity(
        Guid userId,
        string caseKey,
        string rarityKey,
        CancellationToken cancellationToken = default)
    {
        int recorded = await _database.GetScalarSP<int>(
            "sp_case_opening_collection_rarity_completion_record",
            Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_rarity_key", rarityKey)),
            cancellationToken);

        return recorded == 1;
    }

    /// <summary>
    /// Saves a completed contract as one database transaction. The procedure validates that all
    /// input rows still belong to this user before it removes them and adds the server-selected output.
    /// </summary>
    public Task ExecuteCaseOpeningTradeUp(
        Guid userId,
        CaseOpeningTradeUpDbModel tradeUp,
        List<Guid> openingIds,
        CaseOpeningHistoryDbModel output,
        Guid? recipeId,
        bool? isMatch,
        CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_execute",
            Parameters(
                ("p_user_id", userId),
                ("p_trade_up_id", tradeUp.TradeUpId),
                ("p_opening_ids", JsonSerializer.Serialize(openingIds)),
                ("p_input_rarity_key", tradeUp.InputRarityKey),
                ("p_output_rarity_key", tradeUp.OutputRarityKey),
                ("p_output_opening_id", output.OpeningId),
                ("p_output_case_key", output.CaseKey),
                ("p_output_source_item_id", output.SourceItemId),
                ("p_output_item_name", output.Name),
                ("p_output_market_hash_name", output.MarketHashName),
                ("p_output_image_url", output.ImageUrl),
                ("p_output_description", output.Description),
                ("p_output_weapon_name", output.WeaponName),
                ("p_output_pattern_name", output.PatternName),
                ("p_output_paint_index", output.PaintIndex),
                ("p_output_phase", output.Phase),
                ("p_output_rarity_name", output.RarityName),
                ("p_output_rarity_color", output.RarityColor),
                ("p_output_wear", output.Wear),
                ("p_output_is_stat_trak", output.IsStatTrak),
                ("p_output_is_rare_special", output.IsRareSpecial),
                ("p_output_supports_stat_trak", output.SupportsStatTrak),
                ("p_output_min_float", output.MinFloat ?? (object)DBNull.Value),
                ("p_output_max_float", output.MaxFloat ?? (object)DBNull.Value),
                ("p_output_float_value", output.FloatValue ?? (object)DBNull.Value),
                ("p_output_pattern_seed", output.PatternSeed ?? (object)DBNull.Value),
                ("p_output_estimated_price", output.EstimatedPrice ?? (object)DBNull.Value),
                ("p_average_input_float", tradeUp.AverageInputFloat),
                ("p_recipe_id", recipeId.HasValue ? recipeId.Value : (object)DBNull.Value),
                ("p_is_match", isMatch.HasValue ? isMatch.Value : (object)DBNull.Value)),
            cancellationToken);
    }

    public Task<List<CaseOpeningTradeUpRecipeObj>> GetCaseOpeningTradeUpRecipes(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_trade_up_recipes_get", ReadTradeUpRecipe,
            Parameters(("p_user_id", userId)), cancellationToken);
    }

    public Task CreateCaseOpeningTradeUpRecipe(Guid userId, CaseOpeningTradeUpRecipeDbModel recipe, int costStars, long costGbpPence, int recipeSlotCap, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_recipe_create",
            Parameters(
                ("p_user_id", userId),
                ("p_recipe_id", recipe.RecipeId),
                ("p_target_case_key", recipe.TargetCaseKey),
                ("p_target_source_item_id", recipe.TargetSourceItemId),
                ("p_target_item_name", recipe.TargetItemName),
                ("p_target_market_hash_name", recipe.TargetMarketHashName),
                ("p_target_image_url", recipe.TargetImageUrl),
                ("p_target_rarity_key", recipe.TargetRarityKey),
                ("p_target_rarity_name", recipe.TargetRarityName),
                ("p_target_rarity_color", recipe.TargetRarityColor),
                ("p_target_input_rarity_key", recipe.TargetInputRarityKey),
                ("p_target_stat_trak", recipe.TargetStatTrak),
                ("p_target_wears", JsonSerializer.Serialize(recipe.TargetWears)),
                ("p_cost_stars", costStars),
                ("p_cost_gbp_pence", costGbpPence),
                ("p_recipe_slot_cap", recipeSlotCap)),
            cancellationToken);
    }

    public Task SetCaseOpeningTradeUpRecipeActive(Guid userId, Guid recipeId, bool isActive, int recipeSlotCap, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_recipe_set_active",
            Parameters(("p_user_id", userId), ("p_recipe_id", recipeId), ("p_is_active", isActive), ("p_recipe_slot_cap", recipeSlotCap)),
            cancellationToken);
    }

    public Task DeleteCaseOpeningTradeUpRecipe(Guid userId, Guid recipeId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_recipe_delete",
            Parameters(("p_user_id", userId), ("p_recipe_id", recipeId)),
            cancellationToken);
    }

    public Task<List<CaseOpeningTradeUpHoldingObj>> GetCaseOpeningTradeUpHoldings(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_trade_up_holdings_get", ReadTradeUpHolding,
            Parameters(("p_user_id", userId)), cancellationToken);
    }

    public Task CollectCaseOpeningTradeUpHolding(Guid userId, Guid holdingId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_holding_collect",
            Parameters(("p_user_id", userId), ("p_holding_id", holdingId)),
            cancellationToken);
    }

    public Task UpgradeCaseOpeningTradeUpRecipeSlots(Guid userId, int costStars, long costGbpPence, int maximumSlots, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_recipe_slot_upgrade",
            Parameters(("p_user_id", userId), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_maximum_slots", maximumSlots)),
            cancellationToken);
    }

    public Task UpgradeCaseOpeningTradeUpRecipeHolding(Guid userId, Guid recipeId, int costStars, long costGbpPence, int maximumCapacity, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_trade_up_recipe_holding_upgrade",
            Parameters(("p_user_id", userId), ("p_recipe_id", recipeId), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_maximum_capacity", maximumCapacity)),
            cancellationToken);
    }

    public Task<List<CaseOpeningBotServerDbModel>> GetCaseOpeningBotServers(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_bot_servers_get",
            ReadBotServer,
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task<List<CaseOpeningBotDbModel>> GetCaseOpeningBots(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_bots_get",
            ReadBot,
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task PurchaseCaseOpeningBotServer(Guid userId, Guid serverId, int costStars, long costGbpPence, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_bot_server_purchase",
            Parameters(("p_user_id", userId), ("p_server_id", serverId), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence)),
            cancellationToken);
    }

    public Task PurchaseCaseOpeningBot(Guid userId, Guid serverId, Guid botId, int costStars, long costGbpPence, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_bot_purchase",
            Parameters(
                ("p_user_id", userId),
                ("p_server_id", serverId),
                ("p_bot_id", botId),
                ("p_cost_stars", costStars),
                ("p_cost_gbp_pence", costGbpPence)),
            cancellationToken);
    }

    public async Task<bool> ClaimCaseOpeningBotCycle(Guid userId, Guid botId, CancellationToken cancellationToken = default)
    {
        int claimed = await _database.GetScalarSP<int>(
            "sp_case_opening_bot_cycle_claim",
            Parameters(("p_user_id", userId), ("p_bot_id", botId)),
            cancellationToken);

        return claimed == 1;
    }

    /// <summary>
    /// Checks the complete float and pattern pairing for this user and skin. Wear names are broad
    /// bands and can repeat, but an exact simulated item condition should belong to one opening.
    /// </summary>
    public async Task<bool> CaseOpeningConditionExists(
        Guid userId,
        string sourceItemId,
        decimal floatValue,
        int patternSeed,
        CancellationToken cancellationToken = default)
    {
        int count = await _database.GetScalarSP<int>(
            "sp_case_opening_condition_exists",
            Parameters(
                ("p_user_id", userId),
                ("p_source_item_id", sourceItemId),
                ("p_float_value", floatValue),
                ("p_pattern_seed", patternSeed)),
            cancellationToken);

        return count > 0;
    }

    public async Task<bool> CaseOpeningCollectionItemExists(Guid userId, string caseKey, string sourceItemId, CancellationToken cancellationToken = default)
    {
        int count = await _database.GetScalarSP<int>("sp_case_opening_collection_item_exists",
            Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_source_item_id", sourceItemId)), cancellationToken);
        return count > 0;
    }

    public async Task SaveCaseOpening(Guid userId, CaseOpeningHistoryDbModel opening, CancellationToken cancellationToken = default)
    {
        await _database.ExecuteSP(
            "sp_case_opening_history_create",
            Parameters(
                ("p_user_id", userId),
                ("p_opening_id", opening.OpeningId),
                ("p_case_key", opening.CaseKey ?? string.Empty),
                ("p_source_item_id", opening.SourceItemId ?? string.Empty),
                ("p_item_name", opening.Name ?? string.Empty),
                ("p_market_hash_name", opening.MarketHashName ?? string.Empty),
                ("p_image_url", opening.ImageUrl ?? string.Empty),
                ("p_description", opening.Description ?? string.Empty),
                ("p_weapon_name", opening.WeaponName ?? string.Empty),
                ("p_pattern_name", opening.PatternName ?? string.Empty),
                ("p_paint_index", opening.PaintIndex ?? string.Empty),
                ("p_phase", opening.Phase ?? string.Empty),
                ("p_rarity_key", opening.RarityKey ?? string.Empty),
                ("p_rarity_name", opening.RarityName ?? string.Empty),
                ("p_rarity_color", opening.RarityColor ?? string.Empty),
                ("p_wear", opening.Wear ?? string.Empty),
                ("p_is_stat_trak", opening.IsStatTrak),
                ("p_is_rare_special", opening.IsRareSpecial),
                ("p_supports_stat_trak", opening.SupportsStatTrak),
                ("p_min_float", opening.MinFloat ?? (object)DBNull.Value),
                ("p_max_float", opening.MaxFloat ?? (object)DBNull.Value),
                ("p_float_value", opening.FloatValue ?? (object)DBNull.Value),
                ("p_pattern_seed", opening.PatternSeed ?? (object)DBNull.Value),
                ("p_estimated_price", opening.EstimatedPrice ?? (object)DBNull.Value)),
            cancellationToken);
    }

    public async Task<CaseOpeningStatisticsDbModel> GetCaseOpeningStatistics(
        Guid userId,
        string caseKey,
        string targetRarityKey,
        CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_statistics_get",
            ReadStatistics,
            Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_target_rarity_key", targetRarityKey)),
            cancellationToken) ?? new CaseOpeningStatisticsDbModel();
    }

    public Task<CaseOpeningProgressDbModel?> AddCaseOpeningXp(Guid userId, int xpDelta, CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_xp_add",
            ReadProgress,
            Parameters(("p_user_id", userId), ("p_xp_delta", xpDelta)),
            cancellationToken);
    }

    public async Task<CaseOpeningGameSettingsObj> GetGameSettings(CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_game_settings_get",
            ReadGameSettings,
            cancellationToken: cancellationToken) ?? new CaseOpeningGameSettingsObj();
    }

    public async Task SetGameSettings(CaseOpeningGameSettingsObj settings, CancellationToken cancellationToken = default)
    {
        await _database.ExecuteSP(
            "sp_case_opening_game_settings_set",
            Parameters(
                ("p_xp_per_case_open", settings.XpPerCaseOpen),
                ("p_economy_mode", settings.EconomyMode),
                ("p_skin_sale_rate_basis_points", settings.SkinSaleRateBasisPoints),
                ("p_free_case_allowance_enabled", settings.FreeCaseAllowanceEnabled),
                ("p_free_case_allowance_quantity", settings.FreeCaseAllowanceQuantity),
                ("p_free_case_allowance_hours", settings.FreeCaseAllowanceHours),
                ("p_skip_animation_cost_stars", settings.SkipAnimationCostStars),
                ("p_skip_animation_cost_gbp_pence", settings.SkipAnimationCostGbpPence),
                ("p_skip_animation_xp_requirement", settings.SkipAnimationXpRequirement),
                ("p_multi_open_cost_stars", settings.MultiOpenCostStars),
                ("p_multi_open_cost_gbp_pence", settings.MultiOpenCostGbpPence),
                ("p_multi_open_xp_requirement", settings.MultiOpenXpRequirement),
                ("p_open_speed_upgrade_base_cost_stars", settings.OpenSpeedUpgradeBaseCostStars),
                ("p_open_speed_upgrade_base_cost_gbp_pence", settings.OpenSpeedUpgradeBaseCostGbpPence),
                ("p_open_speed_upgrade_cost_increment_stars", settings.OpenSpeedUpgradeCostIncrementStars),
                ("p_open_speed_upgrade_cost_increment_gbp_pence", settings.OpenSpeedUpgradeCostIncrementGbpPence),
                ("p_open_speed_upgrade_xp_requirement", settings.OpenSpeedUpgradeXpRequirement),
                ("p_maximum_open_speed_level", settings.MaximumOpenSpeedLevel),
                ("p_maximum_multi_open_level", settings.MaximumMultiOpenLevel),
                ("p_maximum_open_quantity", settings.MaximumOpenQuantity),
                ("p_bot_opening_interval_seconds", settings.BotOpeningIntervalSeconds),
                ("p_bot_server_base_cost_stars", settings.BotServerBaseCostStars),
                ("p_bot_server_base_cost_gbp_pence", settings.BotServerBaseCostGbpPence),
                ("p_bot_server_cost_increment_stars", settings.BotServerCostIncrementStars),
                ("p_bot_server_cost_increment_gbp_pence", settings.BotServerCostIncrementGbpPence),
                ("p_bot_base_cost_stars", settings.BotBaseCostStars),
                ("p_bot_base_cost_gbp_pence", settings.BotBaseCostGbpPence),
                ("p_bot_speed_upgrade_base_cost_gbp_pence", settings.BotSpeedUpgradeBaseCostGbpPence),
                ("p_bot_speed_upgrade_cost_increment_gbp_pence", settings.BotSpeedUpgradeCostIncrementGbpPence),
                ("p_bot_cost_growth_rate", settings.BotCostGrowthRate),
                ("p_storage_container_base_cost_stars", settings.StorageContainerBaseCostStars),
                ("p_storage_container_base_cost_gbp_pence", settings.StorageContainerBaseCostGbpPence),
                ("p_storage_container_cost_increment_stars", settings.StorageContainerCostIncrementStars),
                ("p_storage_container_cost_increment_gbp_pence", settings.StorageContainerCostIncrementGbpPence),
                ("p_storage_container_slots", settings.StorageContainerSlots),
                ("p_maximum_storage_containers", settings.MaximumStorageContainers),
                ("p_trade_up_recipe_cost_stars", settings.TradeUpRecipeCostStars),
                ("p_trade_up_recipe_cost_gbp_pence", settings.TradeUpRecipeCostGbpPence),
                ("p_trade_up_slot_upgrade_base_cost_stars", settings.TradeUpSlotUpgradeBaseCostStars),
                ("p_trade_up_slot_upgrade_cost_increment_stars", settings.TradeUpSlotUpgradeCostIncrementStars),
                ("p_trade_up_slot_upgrade_base_cost_gbp_pence", settings.TradeUpSlotUpgradeBaseCostGbpPence),
                ("p_trade_up_slot_upgrade_cost_increment_gbp_pence", settings.TradeUpSlotUpgradeCostIncrementGbpPence),
                ("p_trade_up_holding_upgrade_base_cost_stars", settings.TradeUpHoldingUpgradeBaseCostStars),
                ("p_trade_up_holding_upgrade_cost_increment_stars", settings.TradeUpHoldingUpgradeCostIncrementStars),
                ("p_trade_up_holding_upgrade_base_cost_gbp_pence", settings.TradeUpHoldingUpgradeBaseCostGbpPence),
                ("p_trade_up_holding_upgrade_cost_increment_gbp_pence", settings.TradeUpHoldingUpgradeCostIncrementGbpPence)),
            cancellationToken);
        await _database.ExecuteSP(
            "sp_case_opening_global_return_multiplier_set",
            Parameters(("p_basis_points", settings.GlobalReturnMultiplierBasisPoints)),
            cancellationToken);
        await _database.ExecuteSP(
            "sp_case_opening_csfloat_exchange_rate_set",
            Parameters(("p_basis_points", settings.CsFloatUsdToGbpBasisPoints)),
            cancellationToken);
    }

    public Task<List<CaseOpeningCaseSettingsObj>> GetCaseSettings(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_case_settings_get_all", ReadCaseSettings, cancellationToken: cancellationToken);
    }

    public Task<List<CaseOpeningTradeUpHistoryObj>> GetCaseOpeningTradeUpHistory(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_trade_up_history_get", reader => new CaseOpeningTradeUpHistoryObj
        {
            TradeUpId=Guid.Parse(reader.GetString("TradeUpId")),InputRarityKey=reader.GetString("InputRarityKey"),OutputRarityKey=reader.GetString("OutputRarityKey"),OutputCaseKey=reader.GetString("OutputCaseKey"),
            AverageInputFloat=reader.GetDecimal("AverageInputFloat"),CreatedUtc=reader.GetDateTime("CreatedUtc"),OutputName=reader.GetString("OutputName"),OutputImageUrl=reader.GetString("OutputImageUrl"),OutputWear=reader.GetString("OutputWear"),OutputIsStatTrak=reader.GetBoolean("OutputIsStatTrak"),OutputEstimatedPrice=reader.GetDecimal("OutputEstimatedPrice")
        }, Parameters(("p_user_id",userId)), cancellationToken);
    }

    public async Task<bool> GetCaseOpeningSkillTreeEnabled(CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_skill_tree_settings_get",
            reader => reader.GetBoolean("Enabled"),
            cancellationToken: cancellationToken);
    }

    public Task SetCaseOpeningSkillTreeEnabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_skill_tree_settings_set",
            Parameters(("p_enabled", enabled)),
            cancellationToken);
    }

    public async Task<bool> GetGuestAccessEnabled(CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_tycoon_guest_access_get",
            reader => reader.GetBoolean("Enabled"),
            cancellationToken: cancellationToken);
    }

    public Task SetGuestAccessEnabled(bool enabled, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_tycoon_guest_access_set",
            Parameters(("p_enabled", enabled)),
            cancellationToken);
    }

    public Task<List<CaseOpeningTierEconomySettingsObj>> GetTierEconomySettings(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_tier_economy_settings_get",
            reader => new CaseOpeningTierEconomySettingsObj
            {
                Tier = reader.GetInt32("Tier"),
                TargetProfitBasisPoints = reader.GetInt32("TargetProfitBasisPoints"),
                PriceRoundingPence = reader.GetInt32("PriceRoundingPence")
            },
            cancellationToken: cancellationToken);
    }

    public Task SetTierEconomySettings(int tier, int targetProfitBasisPoints, int priceRoundingPence, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_tier_economy_settings_set",
            Parameters(("p_tier", tier), ("p_target_profit_basis_points", targetProfitBasisPoints), ("p_price_rounding_pence", priceRoundingPence)),
            cancellationToken);
    }

    public Task SetCaseSettings(string caseKey, int tier, int unlockCostStars, long unlockCostGbpPence, int purchaseCostStars, long purchaseCostGbpPence, int xpRequirement, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_case_settings_set",
            Parameters(("p_case_key", caseKey), ("p_tier", tier), ("p_unlock_cost_stars", unlockCostStars), ("p_unlock_cost_gbp_pence", unlockCostGbpPence), ("p_purchase_cost_stars", purchaseCostStars), ("p_purchase_cost_gbp_pence", purchaseCostGbpPence), ("p_xp_requirement", xpRequirement)),
            cancellationToken);
    }

    public async Task<CaseOpeningInventoryUpgradeDbModel> GetCaseOpeningInventoryUpgrades(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP("sp_case_opening_inventory_upgrades_get", ReadInventoryUpgrades,
            Parameters(("p_user_id", userId)), cancellationToken) ?? new CaseOpeningInventoryUpgradeDbModel { UserId = userId };
    }

    public Task<List<CaseOpeningUpgradeDefinitionObj>> GetCaseOpeningUpgradeDefinitions(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_upgrade_definitions_get", reader => new CaseOpeningUpgradeDefinitionObj
        {
            UpgradeKey = reader.GetString("UpgradeKey"), Name = reader.GetString("Name"), Description = reader.GetString("Description"),
            Category = reader.GetString("Category"), CostStars = reader.GetInt32("CostStars"), CostGbpPence = reader.GetInt64("CostGbpPence"), RequiredLevel = reader.GetInt32("RequiredLevel"),
            SortOrder = reader.GetInt32("SortOrder"), IsUnlocked = reader.GetBoolean("IsUnlocked")
        }, Parameters(("p_user_id", userId)), cancellationToken);
    }

    public Task UnlockCaseOpeningInventoryUpgrade(Guid userId, string upgradeKey, int costStars, long costGbpPence, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_opening_inventory_upgrade_unlock",
            Parameters(("p_user_id", userId), ("p_upgrade_key", upgradeKey), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence)), cancellationToken);
    }

    public Task<List<CaseOpeningAutoBuyRuleDbModel>> GetCaseOpeningAutoBuyRules(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_auto_buy_rules_get", ReadAutoBuyRule,
            Parameters(("p_user_id", userId)), cancellationToken);
    }

    public Task SetCaseOpeningAutoBuyRule(
        Guid userId,
        string caseKey,
        int thresholdQuantity,
        int purchaseQuantity,
        bool isEnabled,
        int ruleSlotCap,
        CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_auto_buy_rule_set",
            Parameters(
                ("p_user_id", userId),
                ("p_case_key", caseKey),
                ("p_threshold_quantity", thresholdQuantity),
                ("p_purchase_quantity", purchaseQuantity),
                ("p_is_enabled", isEnabled),
                ("p_rule_slot_cap", ruleSlotCap)),
            cancellationToken);
    }

    public Task DeleteCaseOpeningAutoBuyRule(Guid userId, string caseKey, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_auto_buy_rule_delete",
            Parameters(("p_user_id", userId), ("p_case_key", caseKey)),
            cancellationToken);
    }

    public Task<List<CaseOpeningUpgradeDefinitionObj>> GetInventoryUpgradeSettings(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_upgrade_settings_get", ReadUpgradeDefinition, cancellationToken: cancellationToken);
    }

    public Task SetInventoryUpgradeSettings(string upgradeKey, int costStars, long costGbpPence, int requiredLevel, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_opening_upgrade_settings_set",
            Parameters(("p_upgrade_key", upgradeKey), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_required_level", requiredLevel)), cancellationToken);
    }

    public Task SetCaseOpeningAutoSellPreference(Guid userId, string rarityKey, bool enabled, bool preserveStatTrak, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_opening_auto_sell_set",
            Parameters(("p_user_id", userId), ("p_rarity_key", rarityKey), ("p_enabled", enabled), ("p_preserve_stat_trak", preserveStatTrak)), cancellationToken);
    }

    public Task<List<CaseOpeningXpByRarityObj>> GetXpByRarity(CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_opening_xp_by_rarity_get_all", ReadXpByRarity, cancellationToken: cancellationToken);
    }

    public Task SetXpByRarity(string rarityKey, int xpAwarded, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_xp_by_rarity_set",
            Parameters(("p_rarity_key", rarityKey), ("p_xp_awarded", xpAwarded)),
            cancellationToken);
    }

    public Task<CaseOpeningCasePurchaseResultObj?> PurchaseCaseOpeningCases(Guid userId, string caseKey, int quantity, int purchaseCostStars, long purchaseCostGbpPence, CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP("sp_case_opening_cases_purchase", reader => new CaseOpeningCasePurchaseResultObj
        {
            CaseKey = reader.GetString("CaseKey"), PurchasedQuantity = reader.GetInt32("PurchasedQuantity"), OwnedQuantity = reader.GetInt32("OwnedQuantity"),
            StarsSpent = reader.GetInt32("StarsSpent"), StarsBalance = reader.GetInt32("StarsBalance"), EconomyMode = reader.GetString("EconomyMode"),
            AmountSpentMinor = reader.GetInt64("AmountSpentMinor"), BalanceMinor = reader.GetInt64("BalanceMinor")
        }, Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_quantity", quantity), ("p_purchase_cost_stars", purchaseCostStars), ("p_purchase_cost_gbp_pence", purchaseCostGbpPence)), cancellationToken);
    }

    public Task<CaseOpeningStoragePurchaseResultObj?> PurchaseCaseOpeningStorageContainer(Guid userId, Guid storageContainerId, int costStars, long costGbpPence, int slots, int maximumContainers, CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP("sp_case_opening_storage_container_purchase", reader => new CaseOpeningStoragePurchaseResultObj
        {
            StorageContainerCount = reader.GetInt32("StorageContainerCount"), AddedSlots = reader.GetInt32("AddedSlots"), TotalCapacity = reader.GetInt32("TotalCapacity"),
            StarsSpent = reader.GetInt32("StarsSpent"), StarsBalance = reader.GetInt32("StarsBalance"), EconomyMode = reader.GetString("EconomyMode"), AmountSpentMinor = reader.GetInt64("AmountSpentMinor"), BalanceMinor = reader.GetInt64("BalanceMinor")
        }, Parameters(("p_user_id", userId), ("p_storage_container_id", storageContainerId), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_slots", slots), ("p_maximum_containers", maximumContainers)), cancellationToken);
    }

    public Task<CaseOpeningProgressDbModel?> SetCaseOpeningProgressDev(Guid userId, int stars, long gbpPence, int xp, CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP(
            "sp_case_opening_progress_dev_set",
            ReadProgress,
            Parameters(("p_user_id", userId), ("p_stars", stars), ("p_gbp_pence", gbpPence), ("p_xp", xp)),
            cancellationToken);
    }

    public async Task<CaseOpeningProgressDbModel?> SetCaseOpeningUpgradesDev(Guid userId, bool skipAnimationUnlocked, int multiOpenLevel, int openSpeedLevel, CancellationToken cancellationToken = default)
    {
        // Older deployed copies of this admin-only procedure return the pre-dual-economy shape
        // (without GbpPence). Execute the update without mapping its stale result, then reload
        // progress through the canonical procedure which always has the complete shape.
        await _database.ExecuteSP(
            "sp_case_opening_upgrades_dev_set",
            Parameters(
                ("p_user_id", userId),
                ("p_skip_animation_unlocked", skipAnimationUnlocked),
                ("p_multi_open_level", multiOpenLevel),
                ("p_open_speed_level", openSpeedLevel)),
            cancellationToken);
        return await GetCaseOpeningProgress(userId, cancellationToken);
    }

    public async Task<CaseOpeningUserPreferencesObj> GetCaseOpeningUserPreferences(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP("sp_case_opening_user_preferences_get",
            ReadUserPreferences,
            Parameters(("p_user_id", userId)), cancellationToken) ?? new CaseOpeningUserPreferencesObj();
    }

    public async Task<CaseOpeningUserPreferencesObj> SetCaseOpeningAutomationPreferences(Guid userId, CaseOpeningUserPreferencesObj preferences, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP("sp_case_opening_automation_preferences_set", ReadUserPreferences,
            Parameters(("p_user_id",userId),("p_auto_buy_reserve",preferences.AutoBuyReserveMinor),("p_follow_selected",preferences.FollowSelectedCase),("p_selected_case_key",preferences.SelectedCaseKey ?? string.Empty),("p_auto_sell_protect_above",preferences.AutoSellProtectAboveMinor),("p_duplicate_copies",preferences.AutoSellDuplicateCopies),("p_wears",preferences.AutoSellWears),("p_trade_up_reserve",preferences.TradeUpReserve),("p_pause_free_slots",preferences.PauseAutomationFreeSlots)), cancellationToken) ?? preferences;
    }

    private static CaseOpeningUserPreferencesObj ReadUserPreferences(MySqlDataReader reader) => new()
    {
        LastOpenQuantity=reader.GetInt32("LastOpenQuantity"), AutoBuyReserveMinor=reader.GetInt64("AutoBuyReserveMinor"),
        FollowSelectedCase=reader.GetBoolean("FollowSelectedCase"), SelectedCaseKey=reader.IsDBNull(reader.GetOrdinal("SelectedCaseKey")) ? null : reader.GetString("SelectedCaseKey"),
        AutoSellProtectAboveMinor=reader.GetInt64("AutoSellProtectAboveMinor"), AutoSellDuplicateCopies=reader.GetInt32("AutoSellDuplicateCopies"),
        AutoSellWears=reader.GetString("AutoSellWears"), TradeUpReserve=reader.GetInt32("TradeUpReserve"), PauseAutomationFreeSlots=reader.GetInt32("PauseAutomationFreeSlots")
        ,ReactionLayout=reader.GetString("ReactionLayout"),VictoryEmoteKey=reader.IsDBNull(reader.GetOrdinal("VictoryEmoteKey"))?null:reader.GetString("VictoryEmoteKey"),ProfileShowcaseOpeningId=reader.IsDBNull(reader.GetOrdinal("ProfileShowcaseOpeningId"))?null:Guid.Parse(reader.GetString("ProfileShowcaseOpeningId"))
    };

    public async Task<CaseOpeningUserPreferencesObj> SetCaseOpeningSocialPreferences(Guid userId, CaseOpeningUserPreferencesObj preferences, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP("sp_case_opening_social_preferences_set",ReadUserPreferences,Parameters(("p_user_id",userId),("p_layout",preferences.ReactionLayout),("p_victory_emote",preferences.VictoryEmoteKey??string.Empty),("p_showcase_id",preferences.ProfileShowcaseOpeningId?.ToString()??string.Empty)),cancellationToken) ?? preferences;
    }

    public async Task<CaseOpeningUserPreferencesObj> SetCaseOpeningLastQuantity(Guid userId, int quantity, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP("sp_case_opening_last_quantity_set",
            reader => new CaseOpeningUserPreferencesObj { LastOpenQuantity = reader.GetInt32("LastOpenQuantity") },
            Parameters(("p_user_id", userId), ("p_quantity", quantity)), cancellationToken) ?? new CaseOpeningUserPreferencesObj();
    }

    public Task<List<string>> GetCaseBattleReactionUnlocks(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP("sp_case_battle_reaction_unlocks_get", reader => reader.GetString("ReactionKey"),
            Parameters(("p_user_id", userId)), cancellationToken);
    }

    public Task PurchaseCaseBattleReaction(Guid userId, string reactionKey, int costStars, long costGbpPence, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_battle_reaction_purchase",
            Parameters(("p_user_id", userId), ("p_reaction_key", reactionKey), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence)), cancellationToken);
    }

    public Task SetCaseOpeningCaseUnlockDev(Guid userId, string caseKey, bool unlock, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_case_unlock_dev_set",
            Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_unlock", unlock)),
            cancellationToken);
    }

    public Task<CaseOpeningCaseDiscardResultObj?> DiscardCaseOpeningCases(Guid userId, string caseKey, int quantity, CancellationToken cancellationToken = default)
    {
        return _database.GetDataSP("sp_case_opening_cases_discard", reader => new CaseOpeningCaseDiscardResultObj
        {
            CaseKey = reader.GetString("CaseKey"),
            DiscardedQuantity = reader.GetInt32("DiscardedQuantity"),
            OwnedQuantity = reader.GetInt32("OwnedQuantity")
        }, Parameters(("p_user_id", userId), ("p_case_key", caseKey), ("p_quantity", quantity)), cancellationToken);
    }

    public async Task<CaseOpeningDailyDropDbModel> GetCaseOpeningDailyDrop(Guid userId, CancellationToken cancellationToken = default)
        => await _database.GetDataSP("sp_case_opening_daily_drop_get", ReadDailyDrop, Parameters(("p_user_id", userId)), cancellationToken)
           ?? new CaseOpeningDailyDropDbModel();

    public async Task<CaseOpeningDailyDropDbModel> AddCaseOpeningDailyDropXp(Guid userId, int xpDelta, int requiredXp, CancellationToken cancellationToken = default)
        => await _database.GetDataSP("sp_case_opening_daily_drop_xp_add", ReadDailyDrop, Parameters(("p_user_id", userId), ("p_xp_delta", xpDelta), ("p_required_xp", requiredXp)), cancellationToken)
           ?? new CaseOpeningDailyDropDbModel();

    public async Task<CaseOpeningDailyDropDbModel> SetCaseOpeningDailyDropOffer(Guid userId, string offerJson, CancellationToken cancellationToken = default)
        => await _database.GetDataSP("sp_case_opening_daily_drop_offer_set", ReadDailyDrop, Parameters(("p_user_id", userId), ("p_offer", offerJson)), cancellationToken)
           ?? new CaseOpeningDailyDropDbModel();

    public Task ClaimCaseOpeningDailyDrop(Guid userId, List<string> rewardKeys, string economyMode, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_daily_drop_claim", Parameters(("p_user_id", userId), ("p_reward_keys", JsonSerializer.Serialize(rewardKeys)), ("p_economy_mode", economyMode)), cancellationToken);

    public Task<List<CaseOpeningDailyDropUpgradeDbModel>> GetCaseOpeningDailyDropUpgrades(Guid userId, CancellationToken cancellationToken = default)
        => _database.GetBulkDataSP("sp_case_opening_daily_drop_upgrades_get", reader => new CaseOpeningDailyDropUpgradeDbModel { UpgradeKey=reader.GetString("UpgradeKey"), Level=reader.GetInt32("Level") }, Parameters(("p_user_id", userId)), cancellationToken);

    public Task UnlockCaseOpeningDailyDropUpgrade(Guid userId, string upgradeKey, int costStars, long costGbpPence, string economyMode, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_daily_drop_upgrade_unlock", Parameters(("p_user_id", userId), ("p_upgrade_key", upgradeKey), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_economy_mode", economyMode)), cancellationToken);
    public Task<int> GetCaseOpeningDailyDropRequiredXp(CancellationToken cancellationToken = default) => _database.GetDataSP("sp_case_opening_daily_drop_settings_get", reader => reader.GetInt32("RequiredXp"), cancellationToken: cancellationToken);
    public Task SetCaseOpeningDailyDropRequiredXp(int requiredXp, CancellationToken cancellationToken = default) => _database.ExecuteSP("sp_case_opening_daily_drop_settings_set", Parameters(("p_required_xp", requiredXp)), cancellationToken);
    public Task ResetCaseOpeningDailyDrop(Guid userId, CancellationToken cancellationToken = default) => _database.ExecuteSP("sp_case_opening_daily_drop_reset", Parameters(("p_user_id", userId)), cancellationToken);

    public Task<List<string>> GetCaseOpeningDevDropRarityGroups(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.GetBulkDataSP(
            "sp_case_opening_dev_drop_rarities_get",
            reader => reader.GetString("RarityGroup"),
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    public Task SetCaseOpeningDevDropRarityGroups(Guid userId, List<string> rarityGroups, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_dev_drop_rarities_set",
            Parameters(("p_user_id", userId), ("p_rarity_groups", JsonSerializer.Serialize(rarityGroups))),
            cancellationToken);
    }

    public Task ResetCaseOpeningProgressDev(Guid userId, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP(
            "sp_case_opening_reset_dev",
            Parameters(("p_user_id", userId)),
            cancellationToken);
    }

    private static MySqlParameter[] Parameters(params (string Name, object Value)[] values)
    {
        return values.Select(value => new MySqlParameter(value.Name, value.Value)).ToArray();
    }

    private static CaseOpeningHistoryDbModel ReadHistory(MySqlDataReader reader)
    {
        return new CaseOpeningHistoryDbModel
        {
            OpeningId = reader.GetGuid("OpeningId"),
            UserId = reader.GetGuid("UserId"),
            CaseKey = reader.GetString("CaseKey"),
            SourceItemId = reader.GetString("SourceItemId"),
            Name = reader.GetString("ItemName"),
            MarketHashName = reader.GetString("MarketHashName"),
            ImageUrl = reader.GetString("ImageUrl"),
            Description = reader.GetString("Description"),
            WeaponName = reader.GetString("WeaponName"),
            PatternName = reader.GetString("PatternName"),
            PaintIndex = reader.GetString("PaintIndex"),
            Phase = reader.GetString("Phase"),
            RarityKey = reader.GetString("RarityKey"),
            RarityName = reader.GetString("RarityName"),
            RarityColor = reader.GetString("RarityColor"),
            Wear = reader.GetString("Wear"),
            IsStatTrak = reader.GetBoolean("IsStatTrak"),
            IsRareSpecial = reader.GetBoolean("IsRareSpecial"),
            SupportsStatTrak = reader.GetBoolean("SupportsStatTrak"),
            MinFloat = NullableDecimal(reader, "MinFloat"),
            MaxFloat = NullableDecimal(reader, "MaxFloat"),
            FloatValue = NullableDecimal(reader, "FloatValue"),
            PatternSeed = reader.IsDBNull(reader.GetOrdinal("PatternSeed")) ? null : reader.GetInt32("PatternSeed"),
            EstimatedPrice = reader.IsDBNull(reader.GetOrdinal("EstimatedPrice")) ? null : reader.GetDecimal("EstimatedPrice"),
            IsLocked = reader.GetBoolean("IsLocked"),
            // MariaDB DATETIME values have no timezone marker. The column is UTC by contract, so
            // restore that information before JSON serialisation rather than letting browsers read it as local time.
            OpenedUtc = DateTime.SpecifyKind(reader.GetDateTime("OpenedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningProgressDbModel ReadProgress(MySqlDataReader reader)
    {
        return new CaseOpeningProgressDbModel
        {
            UserId = reader.GetGuid("UserId"),
            Stars = reader.GetInt32("Stars"),
            GbpPence = reader.GetInt64("GbpPence"),
            Xp = reader.GetInt32("Xp"),
            SkipAnimationUnlocked = reader.GetBoolean("SkipAnimationUnlocked"),
            MultiOpenLevel = reader.GetInt32("MultiOpenLevel"),
            OpenSpeedLevel = reader.GetInt32("OpenSpeedLevel")
        };
    }

    private static CaseOpeningPlayerStatsDbModel ReadPlayerStats(MySqlDataReader reader)
    {
        return new CaseOpeningPlayerStatsDbModel
        {
            UserId = reader.GetGuid("UserId"),
            TotalCasesOpened = reader.GetInt32("TotalCasesOpened"),
            TotalSkinsObtained = reader.GetInt32("TotalSkinsObtained"),
            TotalTradeUpsCompleted = reader.GetInt32("TotalTradeUpsCompleted"),
            TotalUnlocks = reader.GetInt32("TotalUnlocks"),
            TotalLoginDays = reader.GetInt32("TotalLoginDays"),
            CurrentLoginStreak = reader.GetInt32("CurrentLoginStreak"),
            LongestLoginStreak = reader.GetInt32("LongestLoginStreak"),
            CompletedCollections = reader.GetInt32("CompletedCollections"),
            CompletedRaritySets = reader.GetInt32("CompletedRaritySets"),
            HighestRewardedLevel = reader.GetInt32("HighestRewardedLevel"),
            LastLoginUtcDate = reader.IsDBNull(reader.GetOrdinal("LastLoginUtcDate"))
                ? null
                : DateOnly.FromDateTime(reader.GetDateTime("LastLoginUtcDate")),
            TotalMilSpecPulls = reader.GetInt64("TotalMilSpecPulls"),
            TotalRestrictedPulls = reader.GetInt64("TotalRestrictedPulls"),
            TotalClassifiedPulls = reader.GetInt64("TotalClassifiedPulls"),
            TotalCovertPulls = reader.GetInt64("TotalCovertPulls"),
            TotalRareSpecialPulls = reader.GetInt64("TotalRareSpecialPulls"),
            TotalStatTrakPulls = reader.GetInt64("TotalStatTrakPulls"),
            TotalCasesPurchased = reader.GetInt64("TotalCasesPurchased"),
            TotalCasePurchaseStarsSpent = reader.GetInt64("TotalCasePurchaseStarsSpent"),
            TotalSaleStarsEarned = reader.GetInt64("TotalSaleStarsEarned"),
            TotalPullValueStars = reader.GetInt64("TotalPullValueStars"),
            TotalStarsSpent = reader.GetInt64("TotalStarsSpent"),
            TotalLevelRewardStars = reader.GetInt64("TotalLevelRewardStars"),
            TotalUpgradesPurchased = reader.GetInt64("TotalUpgradesPurchased"),
            TotalGbpPenceSpent = reader.GetInt64("TotalGbpPenceSpent"),
            TotalGbpPenceEarned = reader.GetInt64("TotalGbpPenceEarned")
            ,TotalGbpCasePurchasePenceSpent = reader.GetInt64("TotalGbpCasePurchasePenceSpent")
            ,TotalGbpSalePenceEarned = reader.GetInt64("TotalGbpSalePenceEarned")
            ,TotalGbpLevelRewardPence = reader.GetInt64("TotalGbpLevelRewardPence")
            ,TotalGbpAchievementRewardPence = reader.GetInt64("TotalGbpAchievementRewardPence")
        };
    }

    private static CaseOpeningAchievementDbModel ReadAchievement(MySqlDataReader reader)
    {
        return new CaseOpeningAchievementDbModel
        {
            AchievementKey = reader.GetString("AchievementKey"),
            Name = reader.GetString("Name"),
            Description = reader.GetString("Description"),
            MetricKey = reader.GetString("MetricKey"),
            TargetValue = reader.GetInt32("TargetValue"),
            RewardStars = reader.GetInt32("RewardStars"),
            RewardGbpPence = reader.GetInt64("RewardGbpPence"),
            SortOrder = reader.GetInt32("SortOrder"),
            IsUnlocked = reader.GetBoolean("IsUnlocked"),
            UnlockedUtc = reader.IsDBNull(reader.GetOrdinal("UnlockedUtc"))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime("UnlockedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningInventoryCapacityDbModel ReadInventoryCapacity(MySqlDataReader reader)
    {
        return new CaseOpeningInventoryCapacityDbModel
        {
            SkinSlots = reader.GetInt32("SkinSlots"),
            CaseSlots = reader.GetInt32("CaseSlots"),
            UsedSlots = reader.GetInt32("UsedSlots"),
            BaseCapacity = reader.GetInt32("BaseCapacity"),
            StorageContainerCount = reader.GetInt32("StorageContainerCount"),
            StorageSlots = reader.GetInt32("StorageSlots"),
            UpgradeSlots = reader.GetInt32("UpgradeSlots"),
            TotalCapacity = reader.GetInt32("TotalCapacity"),
            AvailableSlots = reader.GetInt32("AvailableSlots")
        };
    }

    private static CaseOpeningGameSettingsObj ReadGameSettings(MySqlDataReader reader)
    {
        return new CaseOpeningGameSettingsObj
        {
            EconomyMode = reader.GetString("EconomyMode"),
            SkinSaleRateBasisPoints = reader.GetInt32("SkinSaleRateBasisPoints"),
            GlobalReturnMultiplierBasisPoints = reader.GetInt32("GlobalReturnMultiplierBasisPoints"),
            CsFloatUsdToGbpBasisPoints = reader.GetInt32("CsFloatUsdToGbpBasisPoints"),
            FreeCaseAllowanceEnabled = reader.GetBoolean("FreeCaseAllowanceEnabled"),
            FreeCaseAllowanceQuantity = reader.GetInt32("FreeCaseAllowanceQuantity"),
            FreeCaseAllowanceHours = reader.GetInt32("FreeCaseAllowanceHours"),
            XpPerCaseOpen = reader.GetInt32("XpPerCaseOpen"),
            SkipAnimationCostStars = reader.GetInt32("SkipAnimationCostStars"),
            SkipAnimationCostGbpPence = reader.GetInt64("SkipAnimationCostGbpPence"),
            SkipAnimationXpRequirement = reader.GetInt32("SkipAnimationXpRequirement"),
            MultiOpenCostStars = reader.GetInt32("MultiOpenCostStars"),
            MultiOpenCostGbpPence = reader.GetInt64("MultiOpenCostGbpPence"),
            MultiOpenXpRequirement = reader.GetInt32("MultiOpenXpRequirement"),
            OpenSpeedUpgradeBaseCostStars = reader.GetInt32("OpenSpeedUpgradeBaseCostStars"),
            OpenSpeedUpgradeBaseCostGbpPence = reader.GetInt64("OpenSpeedUpgradeBaseCostGbpPence"),
            OpenSpeedUpgradeCostIncrementStars = reader.GetInt32("OpenSpeedUpgradeCostIncrementStars"),
            OpenSpeedUpgradeCostIncrementGbpPence = reader.GetInt64("OpenSpeedUpgradeCostIncrementGbpPence"),
            OpenSpeedUpgradeXpRequirement = reader.GetInt32("OpenSpeedUpgradeXpRequirement"),
            MaximumOpenSpeedLevel = reader.GetInt32("MaximumOpenSpeedLevel"),
            MaximumMultiOpenLevel = reader.GetInt32("MaximumMultiOpenLevel"),
            MaximumOpenQuantity = reader.GetInt32("MaximumOpenQuantity"),
            BotOpeningIntervalSeconds = reader.GetInt32("BotOpeningIntervalSeconds"),
            BotServerBaseCostStars = reader.GetInt32("BotServerBaseCostStars"),
            BotServerBaseCostGbpPence = reader.GetInt64("BotServerBaseCostGbpPence"),
            BotServerCostIncrementStars = reader.GetInt32("BotServerCostIncrementStars"),
            BotServerCostIncrementGbpPence = reader.GetInt64("BotServerCostIncrementGbpPence"),
            BotBaseCostStars = reader.GetInt32("BotBaseCostStars"),
            BotBaseCostGbpPence = reader.GetInt64("BotBaseCostGbpPence"),
            BotSpeedUpgradeBaseCostGbpPence = reader.GetInt64("BotSpeedUpgradeBaseCostGbpPence"),
            BotSpeedUpgradeCostIncrementGbpPence = reader.GetInt64("BotSpeedUpgradeCostIncrementGbpPence"),
            BotCostGrowthRate = reader.GetDecimal("BotCostGrowthRate"),
            StorageContainerBaseCostStars = reader.GetInt32("StorageContainerBaseCostStars"),
            StorageContainerBaseCostGbpPence = reader.GetInt64("StorageContainerBaseCostGbpPence"),
            StorageContainerCostIncrementStars = reader.GetInt32("StorageContainerCostIncrementStars"),
            StorageContainerCostIncrementGbpPence = reader.GetInt64("StorageContainerCostIncrementGbpPence"),
            StorageContainerSlots = reader.GetInt32("StorageContainerSlots"),
            MaximumStorageContainers = reader.GetInt32("MaximumStorageContainers"),
            TradeUpRecipeCostStars = reader.GetInt32("TradeUpRecipeCostStars"),
            TradeUpRecipeCostGbpPence = reader.GetInt64("TradeUpRecipeCostGbpPence"),
            TradeUpSlotUpgradeBaseCostStars = reader.GetInt32("TradeUpSlotUpgradeBaseCostStars"),
            TradeUpSlotUpgradeCostIncrementStars = reader.GetInt32("TradeUpSlotUpgradeCostIncrementStars"),
            TradeUpSlotUpgradeBaseCostGbpPence = reader.GetInt64("TradeUpSlotUpgradeBaseCostGbpPence"),
            TradeUpSlotUpgradeCostIncrementGbpPence = reader.GetInt64("TradeUpSlotUpgradeCostIncrementGbpPence"),
            TradeUpHoldingUpgradeBaseCostStars = reader.GetInt32("TradeUpHoldingUpgradeBaseCostStars"),
            TradeUpHoldingUpgradeCostIncrementStars = reader.GetInt32("TradeUpHoldingUpgradeCostIncrementStars"),
            TradeUpHoldingUpgradeBaseCostGbpPence = reader.GetInt64("TradeUpHoldingUpgradeBaseCostGbpPence"),
            TradeUpHoldingUpgradeCostIncrementGbpPence = reader.GetInt64("TradeUpHoldingUpgradeCostIncrementGbpPence")
        };
    }

    private static CaseOpeningCaseSettingsObj ReadCaseSettings(MySqlDataReader reader)
    {
        return new CaseOpeningCaseSettingsObj
        {
            CaseKey = reader.GetString("CaseKey"),
            Tier = reader.GetInt32("Tier"),
            UnlockCostStars = reader.GetInt32("UnlockCostStars"),
            UnlockCostGbpPence = reader.GetInt64("UnlockCostGbpPence"),
            PurchaseCostStars = reader.GetInt32("PurchaseCostStars"),
            PurchaseCostGbpPence = reader.GetInt64("PurchaseCostGbpPence"),
            XpRequirement = reader.GetInt32("XpRequirement")
        };
    }

    private static CaseOpeningDailyDropDbModel ReadDailyDrop(MySqlDataReader reader)
    {
        return new CaseOpeningDailyDropDbModel
        {
            DropDate = reader.GetDateTime("DropDate"),
            Xp = reader.GetInt32("Xp"),
            IsCompleted = reader.GetBoolean("IsCompleted"),
            IsClaimed = reader.GetBoolean("IsClaimed"),
            OfferJson = reader.IsDBNull(reader.GetOrdinal("OfferJson")) ? string.Empty : reader.GetString("OfferJson")
        };
    }

    private static CaseOpeningUpgradeDefinitionObj ReadUpgradeDefinition(MySqlDataReader reader)
    {
        return new CaseOpeningUpgradeDefinitionObj
        {
            UpgradeKey = reader.GetString("UpgradeKey"),
            Name = reader.GetString("Name"),
            Description = reader.GetString("Description"),
            Category = reader.GetString("Category"),
            CostStars = reader.GetInt32("CostStars"),
            CostGbpPence = reader.GetInt64("CostGbpPence"),
            RequiredLevel = reader.GetInt32("RequiredLevel"),
            SortOrder = reader.GetInt32("SortOrder"),
            IsUnlocked = reader.GetBoolean("IsUnlocked")
        };
    }

    public Task<List<CaseOpeningSpecialVariantRuleDbModel>> GetActiveCaseOpeningSpecialVariantRules(CancellationToken cancellationToken = default)
        => _database.GetBulkDataSP("sp_case_opening_special_variant_rules_active_get", ReadSpecialVariantRule, cancellationToken: cancellationToken);

    public Task SaveCaseOpeningSpecialVariant(Guid openingId, CaseOpeningSpecialVariantRuleDbModel variant, CancellationToken cancellationToken = default)
        => _database.ExecuteSP(
            "sp_case_opening_opening_special_variant_save",
            Parameters(("p_opening_id", openingId), ("p_rule_id", variant.RuleId), ("p_name", variant.Name),
                ("p_tier", variant.Tier), ("p_description", variant.Description), ("p_price_snapshot_id", variant.PriceSnapshotId ?? (object)DBNull.Value),
                ("p_price", variant.Price ?? (object)DBNull.Value)), cancellationToken);

    public Task<List<CaseOpeningSpecialVariantRuleDbModel>> GetCaseOpeningSpecialVariantRules(CancellationToken cancellationToken = default)
        => _database.GetBulkDataSP("sp_case_opening_special_variant_rules_get", ReadSpecialVariantRule, cancellationToken: cancellationToken);

    public Task SaveCaseOpeningSpecialVariantRule(Guid ruleId, CaseOpeningSpecialVariantRuleRequestObj rule, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_special_variant_rule_save", Parameters(
            ("p_rule_id", ruleId), ("p_source_item_id", rule.SourceItemId), ("p_market_hash_name", rule.MarketHashName), ("p_name", rule.Name), ("p_tier", rule.Tier),
            ("p_description", rule.Description), ("p_paint_index", rule.PaintIndex ?? (object)DBNull.Value), ("p_phase", rule.Phase ?? (object)DBNull.Value),
            ("p_pattern_seed", rule.PatternSeed ?? (object)DBNull.Value), ("p_minimum_float", rule.MinimumFloat ?? (object)DBNull.Value),
            ("p_maximum_float", rule.MaximumFloat ?? (object)DBNull.Value), ("p_requires_stat_trak", rule.RequiresStatTrak ?? (object)DBNull.Value), ("p_is_active", rule.IsActive)), cancellationToken);

    public Task<List<CaseOpeningSpecialVariantPriceSnapshotObj>> GetCaseOpeningSpecialVariantPriceSnapshots(CancellationToken cancellationToken = default)
        => _database.GetBulkDataSP("sp_case_opening_special_variant_price_snapshots_get", ReadSpecialVariantSnapshot, cancellationToken: cancellationToken);

    public Task CreateCaseOpeningSpecialVariantPriceSnapshot(CaseOpeningSpecialVariantPriceSnapshotObj snapshot, Dictionary<Guid, decimal> prices, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_special_variant_price_snapshot_create", Parameters(
            ("p_snapshot_id", snapshot.PriceSnapshotId), ("p_name", snapshot.Name), ("p_source", snapshot.Source),
            ("p_prices", JsonSerializer.Serialize(prices.Select(item => new { ruleId = item.Key, price = item.Value })))), cancellationToken);

    public Task ActivateCaseOpeningSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_special_variant_price_snapshot_activate", Parameters(("p_snapshot_id", snapshotId)), cancellationToken);

    public Task DeleteCaseOpeningSpecialVariantPriceSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
        => _database.ExecuteSP("sp_case_opening_special_variant_price_snapshot_delete", Parameters(("p_snapshot_id", snapshotId)), cancellationToken);

    public async Task<CaseOpeningFreeCaseAllowanceObj> GetCaseOpeningFreeCaseAllowance(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _database.GetDataSP(
            "sp_case_opening_free_case_allowance_get",
            reader => new CaseOpeningFreeCaseAllowanceObj
            {
                Remaining = reader.GetInt32("Remaining"),
                Quantity = reader.GetInt32("Quantity"),
                RefreshUtc = reader.IsDBNull(reader.GetOrdinal("RefreshUtc")) ? null : reader.GetDateTime("RefreshUtc")
            },
            Parameters(("p_user_id", userId)),
            cancellationToken) ?? new CaseOpeningFreeCaseAllowanceObj();
    }

    public Task UpgradeCaseOpeningBot(Guid userId, Guid botId, int costStars, long costGbpPence, int maximumLevel, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_opening_bot_speed_upgrade",
            Parameters(("p_user_id", userId), ("p_bot_id", botId), ("p_cost_stars", costStars), ("p_cost_gbp_pence", costGbpPence), ("p_maximum_level", maximumLevel)), cancellationToken);
    }

    public Task SetCaseOpeningBotServerEnabled(Guid userId, Guid serverId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        return _database.ExecuteSP("sp_case_opening_bot_server_enabled_set",
            Parameters(("p_user_id", userId), ("p_server_id", serverId), ("p_is_enabled", isEnabled)), cancellationToken);
    }

    private static CaseOpeningPriceSnapshotDbModel ReadPriceSnapshot(MySqlDataReader reader)
    {
        return new CaseOpeningPriceSnapshotDbModel
        {
            PriceSnapshotId = reader.GetGuid("PriceSnapshotId"),
            Name = reader.GetString("Name"),
            Source = reader.GetString("Source"),
            Currency = reader.GetString("Currency"),
            PriceBasis = reader.GetString("PriceBasis"),
            SourceItemCount = reader.GetInt32("SourceItemCount"),
            MatchedItemCount = reader.GetInt32("MatchedItemCount"),
            IsActive = reader.GetBoolean("IsActive"),
            ImportedUtc = DateTime.SpecifyKind(reader.GetDateTime("ImportedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningSnapshotPriceDbModel ReadSnapshotPrice(MySqlDataReader reader)
    {
        return new CaseOpeningSnapshotPriceDbModel
        {
            PriceSnapshotId = reader.GetGuid("PriceSnapshotId"),
            MarketHashName = reader.GetString("MarketHashName"),
            Price = reader.GetDecimal("Price"),
            MinimumPrice = NullableDecimal(reader, "MinimumPrice"),
            MeanPrice = NullableDecimal(reader, "MeanPrice"),
            MedianPrice = NullableDecimal(reader, "MedianPrice"),
            SuggestedPrice = NullableDecimal(reader, "SuggestedPrice"),
            Quantity = reader.GetInt32("Quantity"),
            IsFallback = reader.GetBoolean("IsFallback"),
            PriceSource = reader.GetString("PriceSource"),
            PriceMethod = reader.GetString("PriceMethod"),
            SourceMarketHashName = reader.GetString("SourceMarketHashName"),
            SourceUpdatedUtc = reader.IsDBNull(reader.GetOrdinal("SourceUpdatedUtc"))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime("SourceUpdatedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningXpByRarityObj ReadXpByRarity(MySqlDataReader reader)
    {
        return new CaseOpeningXpByRarityObj
        {
            RarityKey = reader.GetString("RarityKey"),
            XpAwarded = reader.GetInt32("XpAwarded")
        };
    }

    private static CaseOpeningCollectionDbModel ReadCollection(MySqlDataReader reader)
    {
        return new CaseOpeningCollectionDbModel
        {
            CollectionId = reader.GetGuid("CollectionId"),
            UserId = reader.GetGuid("UserId"),
            CaseKey = reader.GetString("CaseKey"),
            SourceItemId = reader.GetString("SourceItemId"),
            FirstObtainedUtc = DateTime.SpecifyKind(reader.GetDateTime("FirstObtainedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningBotServerDbModel ReadBotServer(MySqlDataReader reader)
    {
        return new CaseOpeningBotServerDbModel
        {
            ServerId = reader.GetGuid("ServerId"),
            UserId = reader.GetGuid("UserId"),
            CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc),
            SpeedLevel = reader.GetInt32("SpeedLevel"),
            IsEnabled = reader.GetBoolean("IsEnabled")
        };
    }

    private static CaseOpeningInventoryUpgradeDbModel ReadInventoryUpgrades(MySqlDataReader reader)
    {
        return new CaseOpeningInventoryUpgradeDbModel
        {
            UserId = reader.GetGuid("UserId"), BulkSellLimit = reader.GetInt32("BulkSellLimit"),
            BonusInventorySlots = reader.GetInt32("BonusInventorySlots"),
            AutoBuyUnlocked = reader.GetBoolean("AutoBuyUnlocked"), AutoBuyRuleSlots = reader.GetInt32("AutoBuyRuleSlots"),
            TradeUpRecipesUnlocked = reader.GetBoolean("TradeUpRecipesUnlocked"),
            TradeUpRecipeSlots = reader.GetInt32("TradeUpRecipeSlots"),
            AutoSellCovertUnlocked = reader.GetBoolean("AutoSellCovertUnlocked"), AutoSellCovertEnabled = reader.GetBoolean("AutoSellCovertEnabled"),
            AutoSellClassifiedUnlocked = reader.GetBoolean("AutoSellClassifiedUnlocked"), AutoSellClassifiedEnabled = reader.GetBoolean("AutoSellClassifiedEnabled"),
            AutoSellRestrictedUnlocked = reader.GetBoolean("AutoSellRestrictedUnlocked"), AutoSellRestrictedEnabled = reader.GetBoolean("AutoSellRestrictedEnabled"),
            AutoSellMilSpecUnlocked = reader.GetBoolean("AutoSellMilSpecUnlocked"), AutoSellMilSpecEnabled = reader.GetBoolean("AutoSellMilSpecEnabled"),
            PreserveStatTrak = reader.GetBoolean("PreserveStatTrak")
        };
    }

    private static CaseOpeningAutoBuyRuleDbModel ReadAutoBuyRule(MySqlDataReader reader)
    {
        return new CaseOpeningAutoBuyRuleDbModel
        {
            CaseKey = reader.GetString("CaseKey"),
            ThresholdQuantity = reader.GetInt32("ThresholdQuantity"),
            PurchaseQuantity = reader.GetInt32("PurchaseQuantity"),
            IsEnabled = reader.GetBoolean("IsEnabled"),
            CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc),
            UpdatedUtc = DateTime.SpecifyKind(reader.GetDateTime("UpdatedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningTradeUpRecipeObj ReadTradeUpRecipe(MySqlDataReader reader)
    {
        return new CaseOpeningTradeUpRecipeObj
        {
            RecipeId = reader.GetGuid("RecipeId"),
            TargetCaseKey = reader.GetString("TargetCaseKey"),
            TargetSourceItemId = reader.GetString("TargetSourceItemId"),
            TargetItemName = reader.GetString("TargetItemName"),
            TargetMarketHashName = reader.GetString("TargetMarketHashName"),
            TargetImageUrl = reader.GetString("TargetImageUrl"),
            TargetRarityKey = reader.GetString("TargetRarityKey"),
            TargetRarityName = reader.GetString("TargetRarityName"),
            TargetRarityColor = reader.GetString("TargetRarityColor"),
            TargetInputRarityKey = reader.GetString("TargetInputRarityKey"),
            TargetStatTrak = reader.GetBoolean("TargetStatTrak"),
            TargetWears = JsonSerializer.Deserialize<List<string>>(reader.GetString("TargetWears")) ?? [],
            IsActive = reader.GetBoolean("IsActive"),
            HoldingCapacity = reader.GetInt32("HoldingCapacity"),
            CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc),
            UpdatedUtc = DateTime.SpecifyKind(reader.GetDateTime("UpdatedUtc"), DateTimeKind.Utc),
            HeldCount = reader.GetInt32("HeldCount"),
            EligibleInputCount = reader.GetInt32("EligibleInputCount")
        };
    }

    private static CaseOpeningTradeUpHoldingObj ReadTradeUpHolding(MySqlDataReader reader)
    {
        return new CaseOpeningTradeUpHoldingObj
        {
            HoldingId = reader.GetGuid("HoldingId"),
            RecipeId = reader.GetGuid("RecipeId"),
            IsMatch = reader.GetBoolean("IsMatch"),
            TargetItemName = reader.GetString("TargetItemName"),
            OpeningId = reader.GetGuid("OpeningId"),
            CaseKey = reader.GetString("CaseKey"),
            SourceItemId = reader.GetString("SourceItemId"),
            Name = reader.GetString("ItemName"),
            MarketHashName = reader.GetString("MarketHashName"),
            ImageUrl = reader.GetString("ImageUrl"),
            Description = reader.GetString("Description"),
            WeaponName = reader.GetString("WeaponName"),
            PatternName = reader.GetString("PatternName"),
            PaintIndex = reader.GetString("PaintIndex"),
            Phase = reader.GetString("Phase"),
            RarityKey = reader.GetString("RarityKey"),
            RarityName = reader.GetString("RarityName"),
            RarityColor = reader.GetString("RarityColor"),
            Wear = reader.GetString("Wear"),
            IsStatTrak = reader.GetBoolean("IsStatTrak"),
            IsRareSpecial = reader.GetBoolean("IsRareSpecial"),
            SupportsStatTrak = reader.GetBoolean("SupportsStatTrak"),
            MinFloat = NullableDecimal(reader, "MinFloat"),
            MaxFloat = NullableDecimal(reader, "MaxFloat"),
            FloatValue = NullableDecimal(reader, "FloatValue"),
            PatternSeed = reader.IsDBNull(reader.GetOrdinal("PatternSeed")) ? null : reader.GetInt32("PatternSeed"),
            SpecialVariantRuleId = reader.IsDBNull(reader.GetOrdinal("SpecialVariantRuleId")) ? null : reader.GetGuid("SpecialVariantRuleId"),
            SpecialVariantName = reader.IsDBNull(reader.GetOrdinal("SpecialVariantName")) ? string.Empty : reader.GetString("SpecialVariantName"),
            SpecialVariantTier = reader.IsDBNull(reader.GetOrdinal("SpecialVariantTier")) ? string.Empty : reader.GetString("SpecialVariantTier"),
            SpecialVariantDescription = reader.IsDBNull(reader.GetOrdinal("SpecialVariantDescription")) ? string.Empty : reader.GetString("SpecialVariantDescription"),
            SpecialVariantPriceSnapshotId = reader.IsDBNull(reader.GetOrdinal("SpecialVariantPriceSnapshotId")) ? null : reader.GetGuid("SpecialVariantPriceSnapshotId"),
            SpecialVariantPrice = NullableDecimal(reader, "SpecialVariantPrice"),
            EstimatedPrice = reader.IsDBNull(reader.GetOrdinal("EstimatedPrice")) ? null : reader.GetDecimal("EstimatedPrice"),
            OpenedUtc = DateTime.SpecifyKind(reader.GetDateTime("OpenedUtc"), DateTimeKind.Utc)
        };
    }

    private static CaseOpeningSpecialVariantRuleDbModel ReadSpecialVariantRule(MySqlDataReader reader)
    {
        return new CaseOpeningSpecialVariantRuleDbModel
        {
            RuleId = reader.GetGuid("RuleId"), SourceItemId = reader.GetString("SourceItemId"), MarketHashName = reader.IsDBNull(reader.GetOrdinal("MarketHashName")) ? string.Empty : reader.GetString("MarketHashName"), Name = reader.GetString("Name"),
            Tier = reader.GetString("Tier"), Description = reader.GetString("Description"),
            PaintIndex = reader.IsDBNull(reader.GetOrdinal("PaintIndex")) ? string.Empty : reader.GetString("PaintIndex"),
            Phase = reader.IsDBNull(reader.GetOrdinal("Phase")) ? string.Empty : reader.GetString("Phase"),
            PatternSeed = reader.IsDBNull(reader.GetOrdinal("PatternSeed")) ? null : reader.GetInt32("PatternSeed"),
            MinimumFloat = NullableDecimal(reader, "MinimumFloat"), MaximumFloat = NullableDecimal(reader, "MaximumFloat"),
            RequiresStatTrak = reader.IsDBNull(reader.GetOrdinal("RequiresStatTrak")) ? null : reader.GetBoolean("RequiresStatTrak"),
            PriceSnapshotId = reader.IsDBNull(reader.GetOrdinal("PriceSnapshotId")) ? null : reader.GetGuid("PriceSnapshotId"),
            Price = NullableDecimal(reader, "Price")
        };
    }

    private static CaseOpeningSpecialVariantPriceSnapshotObj ReadSpecialVariantSnapshot(MySqlDataReader reader) => new()
    {
        PriceSnapshotId = reader.GetGuid("PriceSnapshotId"), Name = reader.GetString("Name"), Source = reader.GetString("Source"),
        IsActive = reader.GetBoolean("IsActive"), ImportedUtc = DateTime.SpecifyKind(reader.GetDateTime("ImportedUtc"), DateTimeKind.Utc)
    };

    private static CaseOpeningBotDbModel ReadBot(MySqlDataReader reader)
    {
        return new CaseOpeningBotDbModel
        {
            BotId = reader.GetGuid("BotId"),
            ServerId = reader.GetGuid("ServerId"),
            UserId = reader.GetGuid("UserId"),
            CreatedUtc = DateTime.SpecifyKind(reader.GetDateTime("CreatedUtc"), DateTimeKind.Utc),
            LastOpenedUtc = reader.IsDBNull(reader.GetOrdinal("LastOpenedUtc"))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime("LastOpenedUtc"), DateTimeKind.Utc),
            SpeedLevel = reader.GetInt32("SpeedLevel")
        };
    }

    private static CaseOpeningSellResultDbModel ReadSellResult(MySqlDataReader reader)
    {
        return new CaseOpeningSellResultDbModel
        {
            StarsAwarded = reader.GetInt32("StarsAwarded"),
            StarsBalance = reader.GetInt32("StarsBalance"),
            SoldItemCount = reader.GetInt32("SoldItemCount"),
            EconomyMode = reader.GetString("EconomyMode"),
            AmountAwardedMinor = reader.GetInt64("AmountAwardedMinor"),
            BalanceMinor = reader.GetInt64("BalanceMinor")
        };
    }

    private static decimal? NullableDecimal(MySqlDataReader reader, string columnName)
    {
        return reader.IsDBNull(reader.GetOrdinal(columnName)) ? null : reader.GetDecimal(columnName);
    }

    private static CaseOpeningStatisticsDbModel ReadStatistics(MySqlDataReader reader)
    {
        return new CaseOpeningStatisticsDbModel
        {
            TotalOpenings = reader.GetInt64("TotalOpenings"),
            TargetPulls = reader.GetInt64("TargetPulls"),
            CurrentDryStreak = reader.GetInt64("CurrentDryStreak"),
            LastTargetOpenedUtc = reader.IsDBNull(reader.GetOrdinal("LastTargetOpenedUtc"))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime("LastTargetOpenedUtc"), DateTimeKind.Utc)
        };
    }
}
