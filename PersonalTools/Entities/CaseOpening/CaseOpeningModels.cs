namespace PersonalTools.Entities.CaseOpening;

public static class CaseOpeningEconomyModes
{
    public const string Stars = "stars";
    public const string Gbp = "gbp";
}

public class CaseOpeningItemObj
{
    public string SourceItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MarketHashName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WeaponName { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public string PaintIndex { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string RarityKey { get; set; } = string.Empty;
    public string RarityName { get; set; } = string.Empty;
    public string RarityColor { get; set; } = string.Empty;
    public string Wear { get; set; } = string.Empty;
    public bool IsStatTrak { get; set; }
    public bool IsRareSpecial { get; set; }
    public bool SupportsStatTrak { get; set; }
    public decimal? MinFloat { get; set; }
    public decimal? MaxFloat { get; set; }
    public decimal? FloatValue { get; set; }
    public int? PatternSeed { get; set; }
    public Guid? SpecialVariantRuleId { get; set; }
    public string SpecialVariantName { get; set; } = string.Empty;
    public string SpecialVariantTier { get; set; } = string.Empty;
    public string SpecialVariantDescription { get; set; } = string.Empty;
    public Guid? SpecialVariantPriceSnapshotId { get; set; }
    public decimal? SpecialVariantPrice { get; set; }

    // Kept nullable until a shared market-price provider is configured. The API contract
    // can gain prices later without changing the case result or history shapes.
    public decimal? EstimatedPrice { get; set; }
}

public sealed class CaseOpeningCaseObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int UnlockCostStars { get; set; }
    public int PurchaseCostStars { get; set; }
    public long UnlockCostGbpPence { get; set; }
    public long PurchaseCostGbpPence { get; set; }
    public int Tier { get; set; } = 1;
    public long UnlockCost { get; set; }
    public long PurchaseCost { get; set; }
    public int XpRequirement { get; set; }
    public int SaleMultiplier { get; set; } = 1;
    public bool IsUnlocked { get; set; }
    public int OwnedQuantity { get; set; }
    public List<CaseOpeningOddsObj> Odds { get; set; } = [];
    public List<CaseOpeningItemObj> Items { get; set; } = [];
}

public sealed class CaseOpeningCaseSummaryObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int UnlockCostStars { get; set; }
    public int PurchaseCostStars { get; set; }
    public long UnlockCostGbpPence { get; set; }
    public long PurchaseCostGbpPence { get; set; }
    public int Tier { get; set; } = 1;
    public long UnlockCost { get; set; }
    public long PurchaseCost { get; set; }
    public int XpRequirement { get; set; }
    public int SaleMultiplier { get; set; } = 1;
    public bool IsUnlocked { get; set; }
    public int OwnedQuantity { get; set; }
}

public sealed class CaseOpeningOddsObj
{
    public string RarityKey { get; set; } = string.Empty;
    public string RarityName { get; set; } = string.Empty;
    public string RarityColor { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public sealed class CaseOpeningResultObj
{
    public Guid OpeningId { get; set; }
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public CaseOpeningItemObj Winner { get; set; } = new();
    public List<CaseOpeningItemObj> Reel { get; set; } = [];
    public int WinnerIndex { get; set; }
    public int XpAwarded { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public bool LeveledUp { get; set; }
    public int LevelRewardStars { get; set; }
    public long LevelRewardAmountMinor { get; set; }
    public bool IsAutoSold { get; set; }
    public int AutoSoldStars { get; set; }
    public long AutoSoldAmountMinor { get; set; }
    public bool IsNewCollectionItem { get; set; }
    public decimal? MarketOpeningCost { get; set; }
    public decimal? MarketProfit { get; set; }
}

public sealed class CaseOpeningPriceSnapshotDbModel
{
    public Guid PriceSnapshotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Currency { get; set; } = "GBP";
    public string PriceBasis { get; set; } = "median";
    public int SourceItemCount { get; set; }
    public int MatchedItemCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime ImportedUtc { get; set; }
}

public sealed class CaseOpeningSnapshotPriceDbModel
{
    public Guid PriceSnapshotId { get; set; }
    public string MarketHashName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? MeanPrice { get; set; }
    public decimal? MedianPrice { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime? SourceUpdatedUtc { get; set; }
    public bool IsFallback { get; set; }
    public string PriceSource { get; set; } = "Skinport";
    public string PriceMethod { get; set; } = string.Empty;
    public string SourceMarketHashName { get; set; } = string.Empty;
}

public sealed class CaseOpeningCaseMarketValueObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public decimal? OpeningCost { get; set; }
    public decimal? ExpectedValue { get; set; }
    public decimal? ExpectedProfit { get; set; }
    public decimal InferredExpectedValue { get; set; }
    public decimal InferredValuePercentage { get; set; }
    public string ContainerPriceMethod { get; set; } = string.Empty;
    public string PriceQualityWarning { get; set; } = string.Empty;
    public decimal? ReturnPercentage { get; set; }
    public decimal? ExpectedSaleValuePence { get; set; }
    public decimal? TargetReturnPercentage { get; set; }
    public int PricedVariants { get; set; }
    public int TotalVariants { get; set; }
    public int Tier { get; set; }
    public int PublishedTier { get; set; }
    public int RecommendedTier { get; set; }
    public long RecommendedPurchaseGbpPence { get; set; }
    public long RecommendedUnlockGbpPence { get; set; }
    public int RecommendedPurchaseStars { get; set; }
    public int RecommendedUnlockStars { get; set; }
    public long PublishedPurchaseGbpPence { get; set; }
    public bool HasCompletePricing { get; set; }
    public bool HasPublishablePricing { get; set; }
}

public sealed class CaseOpeningPriceSnapshotSummaryObj
{
    public List<CaseOpeningPriceSnapshotDbModel> Snapshots { get; set; } = [];
    public Guid? ActiveSnapshotId { get; set; }
    public string Currency { get; set; } = "GBP";
    public List<CaseOpeningCaseMarketValueObj> Cases { get; set; } = [];
    public bool CanPublish { get; set; }
    public int FallbackPriceCount { get; set; }
    public int MissingPriceCount { get; set; }
    public int MissingContainerPriceCount { get; set; }
    public int PriceQualityWarningCount { get; set; }
    public int SkinportPriceCount { get; set; }
    public int CsFloatPriceCount { get; set; }
    public int InferredPriceCount { get; set; }
    public List<string> TierWarnings { get; set; } = [];
}

public sealed class CaseOpeningOpenRequestObj
{
    public int Quantity { get; set; } = 1;
}

public sealed class CaseOpeningOpenBatchResultObj
{
    public List<CaseOpeningResultObj> Results { get; set; } = [];
    public int RemainingCaseQuantity { get; set; }
}

public sealed class CaseOpeningDevDropSettingsObj
{
    public List<string> RarityGroups { get; set; } = [];
}

public sealed class CaseOpeningOwnedCaseDbModel
{
    public string CaseKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CaseOpeningInventoryCapacityDbModel
{
    public int SkinSlots { get; set; }
    public int CaseSlots { get; set; }
    public int UsedSlots { get; set; }
    public int BaseCapacity { get; set; }
    public int StorageContainerCount { get; set; }
    public int StorageSlots { get; set; }
    public int UpgradeSlots { get; set; }
    public int TotalCapacity { get; set; }
    public int AvailableSlots { get; set; }
}

public sealed class CaseOpeningInventoryCapacityObj : CaseOpeningInventoryCapacityDbModel
{
}

public class CaseOpeningHistoryObj : CaseOpeningItemObj
{
    public Guid OpeningId { get; set; }
    public string CaseKey { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime OpenedUtc { get; set; }
}

public sealed class CaseOpeningHistoryDbModel : CaseOpeningHistoryObj
{
    public Guid UserId { get; set; }
}

public sealed class CaseOpeningCollectionDbModel
{
    public Guid CollectionId { get; set; }
    public Guid UserId { get; set; }
    public string CaseKey { get; set; } = string.Empty;
    public string SourceItemId { get; set; } = string.Empty;
    public DateTime FirstObtainedUtc { get; set; }
}

public sealed class CaseOpeningCollectionItemObj : CaseOpeningItemObj
{
    public bool IsCollected { get; set; }
    public DateTime? FirstObtainedUtc { get; set; }
}

public sealed class CaseOpeningCollectionObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public int TotalItemCount { get; set; }
    public int CollectedItemCount { get; set; }
    public List<CaseOpeningCollectionItemObj> Items { get; set; } = [];
}

public sealed class CaseOpeningSpecialVariantRuleDbModel
{
    public Guid RuleId { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
    public string MarketHashName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PaintIndex { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public int? PatternSeed { get; set; }
    public decimal? MinimumFloat { get; set; }
    public decimal? MaximumFloat { get; set; }
    public bool? RequiresStatTrak { get; set; }
    public Guid? PriceSnapshotId { get; set; }
    public decimal? Price { get; set; }
}

public sealed class CaseOpeningSpecialVariantRuleRequestObj
{
    public string SourceItemId { get; set; } = string.Empty;
    public string MarketHashName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PaintIndex { get; set; }
    public string? Phase { get; set; }
    public int? PatternSeed { get; set; }
    public decimal? MinimumFloat { get; set; }
    public decimal? MaximumFloat { get; set; }
    public bool? RequiresStatTrak { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CaseOpeningSpecialVariantListingEvidenceObj
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string ListingId { get; set; } = string.Empty;
    public decimal PriceMinor { get; set; }
    public decimal FloatValue { get; set; }
    public int PatternSeed { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class CaseOpeningSpecialVariantPriceSnapshotObj
{
    public Guid PriceSnapshotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime ImportedUtc { get; set; }
}

public sealed class CaseOpeningSpecialVariantPriceSnapshotRequestObj
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Dictionary<Guid, decimal> Prices { get; set; } = [];
}

public sealed class CaseOpeningSpecialVariantAdminSummaryObj
{
    public List<CaseOpeningSpecialVariantRuleDbModel> Rules { get; set; } = [];
    public List<CaseOpeningSpecialVariantPriceSnapshotObj> Snapshots { get; set; } = [];
    public Guid? ActiveSnapshotId { get; set; }
}

public sealed class CaseOpeningCollectionRaritySummaryObj
{
    public string RarityKey { get; set; } = string.Empty;
    public string RarityName { get; set; } = string.Empty;
    public string RarityColor { get; set; } = string.Empty;
    public int TotalItemCount { get; set; }
    public int CollectedItemCount { get; set; }
}

public sealed class CaseOpeningCollectionSummaryObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int TotalItemCount { get; set; }
    public int CollectedItemCount { get; set; }
    public DateTime FirstObtainedUtc { get; set; }
    public List<CaseOpeningCollectionRaritySummaryObj> Rarities { get; set; } = [];
}

public class CaseOpeningProgressDbModel
{
    public Guid UserId { get; set; }
    public int Stars { get; set; }
    public long GbpPence { get; set; }
    public int Xp { get; set; }
    public bool SkipAnimationUnlocked { get; set; }
    public int MultiOpenLevel { get; set; }
    public int OpenSpeedLevel { get; set; }
}

public sealed class CaseOpeningProgressObj : CaseOpeningProgressDbModel
{
    public CaseOpeningDailyDropObj DailyDrop { get; set; } = new();
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public string CurrencyCode { get; set; } = "STAR";
    public long ActiveBalanceMinor { get; set; }
    public int SkinSaleRateBasisPoints { get; set; } = 9250;
    public bool FreeCaseAllowanceEnabled { get; set; }
    public int FreeCaseAllowanceRemaining { get; set; }
    public int FreeCaseAllowanceQuantity { get; set; }
    public DateTime? FreeCaseAllowanceRefreshUtc { get; set; }
    public int Level { get; set; }
    public int XpIntoLevel { get; set; }
    public int XpForNextLevel { get; set; }
    public long SkipAnimationCost { get; set; }
    public int SkipAnimationXpRequirement { get; set; }
    public long MultiOpenCost { get; set; }
    public int MultiOpenXpRequirement { get; set; }
    public int MaximumMultiOpenLevel { get; set; }
    public decimal OpenSpeedMultiplier { get; set; }
    public long OpenSpeedUpgradeCost { get; set; }
    public int OpenSpeedUpgradeXpRequirement { get; set; }
    public int MaximumOpenSpeedLevel { get; set; }
    public int MaximumOpenQuantity { get; set; }
    public int StorageContainerBaseCostStars { get; set; }
    public int StorageContainerCostIncrementStars { get; set; }
    public long StorageContainerBaseCost { get; set; }
    public long StorageContainerCostIncrement { get; set; }
    public int StorageContainerSlots { get; set; }
    public int MaximumStorageContainers { get; set; }
    public Dictionary<string, int> SaleValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> CaseSaleMultipliers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> UnlockedCaseKeys { get; set; } = [];
}

// These totals are never inferred from the current inventory. Selling or using a skin in a
// contract changes the inventory, not the player's earned progression history.
public sealed class CaseOpeningPlayerStatsDbModel
{
    public Guid UserId { get; set; }
    public int TotalCasesOpened { get; set; }
    public int TotalSkinsObtained { get; set; }
    public int TotalTradeUpsCompleted { get; set; }
    public int TotalUnlocks { get; set; }
    public int TotalLoginDays { get; set; }
    public int CurrentLoginStreak { get; set; }
    public int LongestLoginStreak { get; set; }
    public int CompletedCollections { get; set; }
    public int CompletedRaritySets { get; set; }
    public int HighestRewardedLevel { get; set; }
    public DateOnly? LastLoginUtcDate { get; set; }
    public long TotalMilSpecPulls { get; set; }
    public long TotalRestrictedPulls { get; set; }
    public long TotalClassifiedPulls { get; set; }
    public long TotalCovertPulls { get; set; }
    public long TotalRareSpecialPulls { get; set; }
    public long TotalStatTrakPulls { get; set; }
    public long TotalCasesPurchased { get; set; }
    public long TotalCasePurchaseStarsSpent { get; set; }
    public long TotalSaleStarsEarned { get; set; }
    public long TotalPullValueStars { get; set; }
    public long TotalStarsSpent { get; set; }
    public long TotalLevelRewardStars { get; set; }
    public long TotalUpgradesPurchased { get; set; }
    public long TotalGbpPenceSpent { get; set; }
    public long TotalGbpPenceEarned { get; set; }
    public long TotalGbpCasePurchasePenceSpent { get; set; }
    public long TotalGbpSalePenceEarned { get; set; }
    public long TotalGbpLevelRewardPence { get; set; }
    public long TotalGbpAchievementRewardPence { get; set; }
}

public sealed class CaseOpeningDailyDropDbModel
{
    public DateTime DropDate { get; set; }
    public int Xp { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }
    public string OfferJson { get; set; } = string.Empty;
}

public sealed class CaseOpeningDailyDropObj
{
    public DateTime DropDate { get; set; }
    public int Xp { get; set; }
    public int RequiredXp { get; set; } = 100;
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }
    public List<CaseOpeningDailyDropRewardObj> Rewards { get; set; } = [];
    public List<CaseOpeningDailyDropUpgradeObj> Upgrades { get; set; } = [];
}

public sealed class CaseOpeningDailyDropUpgradeDbModel
{
    public string UpgradeKey { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class CaseOpeningDailyDropRewardObj
{
    public string RewardKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public long AmountMinor { get; set; }
    public string? CaseKey { get; set; }
    public CaseOpeningItemObj? Item { get; set; }
}

public sealed class CaseOpeningDailyDropUpgradeObj
{
    public string UpgradeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MaximumLevel { get; set; }
    public long Cost { get; set; }
}

public sealed class CaseOpeningFreeCaseAllowanceObj
{
    public int Remaining { get; set; }
    public int Quantity { get; set; }
    public DateTime? RefreshUtc { get; set; }
}

public sealed class CaseOpeningPlayerStatsObj
{
    public int TotalCasesOpened { get; set; }
    public int TotalSkinsObtained { get; set; }
    public int TotalTradeUpsCompleted { get; set; }
    public int TotalUnlocks { get; set; }
    public int TotalLoginDays { get; set; }
    public int CurrentLoginStreak { get; set; }
    public int LongestLoginStreak { get; set; }
    public int CompletedCollections { get; set; }
    public int CompletedRaritySets { get; set; }
    public int HighestRewardedLevel { get; set; }
    public DateOnly? LastLoginUtcDate { get; set; }
    public long TotalMilSpecPulls { get; set; }
    public long TotalRestrictedPulls { get; set; }
    public long TotalClassifiedPulls { get; set; }
    public long TotalCovertPulls { get; set; }
    public long TotalRareSpecialPulls { get; set; }
    public long TotalStatTrakPulls { get; set; }
    public long TotalCasesPurchased { get; set; }
    public long TotalCasePurchaseStarsSpent { get; set; }
    public long TotalSaleStarsEarned { get; set; }
    public long TotalPullValueStars { get; set; }
    public long TotalStarsSpent { get; set; }
    public long TotalLevelRewardStars { get; set; }
    public long TotalUpgradesPurchased { get; set; }
    public long TotalGbpPenceSpent { get; set; }
    public long TotalGbpPenceEarned { get; set; }
    public long TotalGbpCasePurchasePenceSpent { get; set; }
    public long TotalGbpSalePenceEarned { get; set; }
    public long TotalGbpLevelRewardPence { get; set; }
    public long TotalGbpAchievementRewardPence { get; set; }
}

public sealed class CaseOpeningAchievementDbModel
{
    public string AchievementKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MetricKey { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public int RewardStars { get; set; }
    public long RewardGbpPence { get; set; }
    public int SortOrder { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedUtc { get; set; }
}

public sealed class CaseOpeningAchievementObj
{
    public string AchievementKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public int RewardStars { get; set; }
    public long RewardGbpPence { get; set; }
    public long RewardAmountMinor { get; set; }
    public int SortOrder { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedUtc { get; set; }
}

public sealed class CaseOpeningAchievementSummaryObj
{
    public CaseOpeningPlayerStatsObj Stats { get; set; } = new();
    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }
    public int EarnedStars { get; set; }
    public long EarnedAmountMinor { get; set; }
    public List<CaseOpeningAchievementObj> Achievements { get; set; } = [];
}

// Global, shared across every account - one singleton row (Id is always 1).
public sealed class CaseOpeningGameSettingsObj
{
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public int SkinSaleRateBasisPoints { get; set; } = 9250;
    public int GlobalReturnMultiplierBasisPoints { get; set; } = 10300;
    public int CsFloatUsdToGbpBasisPoints { get; set; } = 7800;
    public bool FreeCaseAllowanceEnabled { get; set; }
    public int FreeCaseAllowanceQuantity { get; set; } = 25;
    public int FreeCaseAllowanceHours { get; set; } = 24;
    public int XpPerCaseOpen { get; set; }
    public int SkipAnimationCostStars { get; set; }
    public int SkipAnimationXpRequirement { get; set; }
    public int MultiOpenCostStars { get; set; }
    public int MultiOpenXpRequirement { get; set; }
    public int OpenSpeedUpgradeBaseCostStars { get; set; }
    public int OpenSpeedUpgradeCostIncrementStars { get; set; }
    public int OpenSpeedUpgradeXpRequirement { get; set; }
    public int MaximumOpenSpeedLevel { get; set; }
    public int MaximumMultiOpenLevel { get; set; }
    public int MaximumOpenQuantity { get; set; }
    public int BotOpeningIntervalSeconds { get; set; }
    public int BotServerBaseCostStars { get; set; }
    public int BotServerCostIncrementStars { get; set; }
    public int BotBaseCostStars { get; set; }
    public decimal BotCostGrowthRate { get; set; }
    public int StorageContainerBaseCostStars { get; set; }
    public int StorageContainerCostIncrementStars { get; set; }
    public int StorageContainerSlots { get; set; }
    public int MaximumStorageContainers { get; set; }
    public int TradeUpRecipeCostStars { get; set; }
    public long SkipAnimationCostGbpPence { get; set; }
    public long MultiOpenCostGbpPence { get; set; }
    public long OpenSpeedUpgradeBaseCostGbpPence { get; set; }
    public long OpenSpeedUpgradeCostIncrementGbpPence { get; set; }
    public long BotServerBaseCostGbpPence { get; set; }
    public long BotServerCostIncrementGbpPence { get; set; }
    public long BotBaseCostGbpPence { get; set; }
    public long BotSpeedUpgradeBaseCostGbpPence { get; set; } = 300;
    public long BotSpeedUpgradeCostIncrementGbpPence { get; set; } = 100;
    public long StorageContainerBaseCostGbpPence { get; set; }
    public long StorageContainerCostIncrementGbpPence { get; set; }
    public long TradeUpRecipeCostGbpPence { get; set; }
    public int TradeUpSlotUpgradeBaseCostStars { get; set; } = 300;
    public int TradeUpSlotUpgradeCostIncrementStars { get; set; } = 75;
    public long TradeUpSlotUpgradeBaseCostGbpPence { get; set; } = 300;
    public long TradeUpSlotUpgradeCostIncrementGbpPence { get; set; } = 75;
    public int TradeUpHoldingUpgradeBaseCostStars { get; set; } = 250;
    public int TradeUpHoldingUpgradeCostIncrementStars { get; set; } = 50;
    public long TradeUpHoldingUpgradeBaseCostGbpPence { get; set; } = 250;
    public long TradeUpHoldingUpgradeCostIncrementGbpPence { get; set; } = 50;
}

public sealed class CaseOpeningCaseSettingsObj
{
    public string CaseKey { get; set; } = string.Empty;
    public int UnlockCostStars { get; set; }
    public int PurchaseCostStars { get; set; }
    public long UnlockCostGbpPence { get; set; }
    public long PurchaseCostGbpPence { get; set; }
    public int Tier { get; set; } = 1;
    public int XpRequirement { get; set; }
}

public sealed class CaseOpeningXpByRarityObj
{
    public string RarityKey { get; set; } = string.Empty;
    public int XpAwarded { get; set; }
}

public sealed class CaseOpeningCasePurchaseRequestObj
{
    public int Quantity { get; set; } = 1;
}

public sealed class CaseOpeningTierEconomySettingsObj
{
    public int Tier { get; set; }
    public int TargetProfitBasisPoints { get; set; }
    public int PriceRoundingPence { get; set; } = 5;
}

public sealed class CaseOpeningCaseDiscardRequestObj
{
    // Zero represents the player's entire current stock and is resolved atomically in MariaDB.
    public int Quantity { get; set; }
}

public sealed class CaseOpeningCasePurchaseResultObj
{
    public string CaseKey { get; set; } = string.Empty;
    public int PurchasedQuantity { get; set; }
    public int OwnedQuantity { get; set; }
    public int StarsSpent { get; set; }
    public int StarsBalance { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long AmountSpentMinor { get; set; }
    public long BalanceMinor { get; set; }
}

public sealed class CaseOpeningStoragePurchaseResultObj
{
    public int StorageContainerCount { get; set; }
    public int AddedSlots { get; set; }
    public int TotalCapacity { get; set; }
    public int StarsSpent { get; set; }
    public int StarsBalance { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long AmountSpentMinor { get; set; }
    public long BalanceMinor { get; set; }
}

public sealed class CaseOpeningSellRequestObj
{
    public List<Guid> OpeningIds { get; set; } = [];
}

public sealed class CaseOpeningCaseDiscardResultObj
{
    public string CaseKey { get; set; } = string.Empty;
    public int DiscardedQuantity { get; set; }
    public int OwnedQuantity { get; set; }
}

public sealed class CaseOpeningInventoryLockRequestObj
{
    public bool IsLocked { get; set; }
}

public sealed class CaseOpeningInventoryLockObj
{
    public Guid OpeningId { get; set; }
    public bool IsLocked { get; set; }
}

public class CaseOpeningSellResultObj
{
    public int StarsAwarded { get; set; }
    public int StarsBalance { get; set; }
    public int SoldItemCount { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long AmountAwardedMinor { get; set; }
    public long BalanceMinor { get; set; }
}

public sealed class CaseOpeningSellResultDbModel : CaseOpeningSellResultObj
{
}

public class CaseOpeningInventoryUpgradeDbModel
{
    public Guid UserId { get; set; }
    public int BulkSellLimit { get; set; } = 100;
    public int BonusInventorySlots { get; set; }
    public bool AutoBuyUnlocked { get; set; }
    public int AutoBuyRuleSlots { get; set; } = 3;
    public bool TradeUpRecipesUnlocked { get; set; }
    public int TradeUpRecipeSlots { get; set; }
    public bool AutoSellCovertUnlocked { get; set; }
    public bool AutoSellCovertEnabled { get; set; }
    public bool AutoSellClassifiedUnlocked { get; set; }
    public bool AutoSellClassifiedEnabled { get; set; }
    public bool AutoSellRestrictedUnlocked { get; set; }
    public bool AutoSellRestrictedEnabled { get; set; }
    public bool AutoSellMilSpecUnlocked { get; set; }
    public bool AutoSellMilSpecEnabled { get; set; }
    public bool PreserveStatTrak { get; set; } = true;
}

public sealed class CaseOpeningInventoryUpgradeObj : CaseOpeningInventoryUpgradeDbModel
{
    public int Stars { get; set; }
    public long GbpPence { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long ActiveBalanceMinor { get; set; }
    public List<CaseOpeningUpgradeDefinitionObj> AvailableUpgrades { get; set; } = [];

    // Recipe slots are a repeatable +1 purchase (like the bot speed upgrade) rather than a
    // discrete-tier upgrade definition, so the cost is computed here instead of living in
    // CaseOpeningUpgradeDefinitions. Zero means "not unlocked yet" or "already at maximum".
    public int TradeUpRecipeSlotUpgradeCostStars { get; set; }
    public long TradeUpRecipeSlotUpgradeCost { get; set; }
    public int MaximumTradeUpRecipeSlots { get; set; }
}

public sealed class CaseOpeningUpgradeDefinitionObj
{
    public string UpgradeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CostStars { get; set; }
    public long CostGbpPence { get; set; }
    public long Cost { get; set; }
    public int RequiredLevel { get; set; }
    public int SortOrder { get; set; }
    public bool IsUnlocked { get; set; }
}

public sealed class CaseOpeningAutoSellPreferenceRequestObj
{
    public string RarityKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool? PreserveStatTrak { get; set; }
}

public class CaseOpeningAutoBuyRuleDbModel
{
    public string CaseKey { get; set; } = string.Empty;
    public int ThresholdQuantity { get; set; }
    public int PurchaseQuantity { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CaseOpeningAutoBuyRuleObj : CaseOpeningAutoBuyRuleDbModel
{
    public string CaseName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int OwnedQuantity { get; set; }
}

public sealed class CaseOpeningAutoBuyRuleRequestObj
{
    public int ThresholdQuantity { get; set; }
    public int PurchaseQuantity { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
}

public sealed class CaseOpeningAutoBuySummaryObj
{
    public bool Unlocked { get; set; }
    public int RuleSlots { get; set; }
    public int UsedRuleSlots { get; set; }
    public List<CaseOpeningAutoBuyRuleObj> Rules { get; set; } = [];
}

public sealed class CaseOpeningTradeUpRequestObj
{
    public List<Guid> OpeningIds { get; set; } = [];
}

public sealed class CaseOpeningTradeUpSourceChanceObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public int InputCount { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class CaseOpeningTradeUpResultObj
{
    public Guid TradeUpId { get; set; }
    public string InputRarityName { get; set; } = string.Empty;
    public string OutputRarityName { get; set; } = string.Empty;
    public decimal AverageInputFloat { get; set; }
    public CaseOpeningHistoryObj Output { get; set; } = new();
    public List<CaseOpeningTradeUpSourceChanceObj> SourceChances { get; set; } = [];

    // Set only when this contract was fired by an auto trade-up recipe rather than manually.
    public Guid? RecipeId { get; set; }
    public bool? IsMatch { get; set; }
}

public sealed class CaseOpeningTradeUpDbModel
{
    public Guid TradeUpId { get; set; }
    public Guid UserId { get; set; }
    public string InputRarityKey { get; set; } = string.Empty;
    public string OutputRarityKey { get; set; } = string.Empty;
    public Guid OutputOpeningId { get; set; }
    public string OutputCaseKey { get; set; } = string.Empty;
    public decimal AverageInputFloat { get; set; }
}

public class CaseOpeningTradeUpRecipeDbModel
{
    public Guid RecipeId { get; set; }
    public string TargetCaseKey { get; set; } = string.Empty;
    public string TargetSourceItemId { get; set; } = string.Empty;
    public string TargetItemName { get; set; } = string.Empty;
    public string TargetMarketHashName { get; set; } = string.Empty;
    public string TargetImageUrl { get; set; } = string.Empty;
    public string TargetRarityKey { get; set; } = string.Empty;
    public string TargetRarityName { get; set; } = string.Empty;
    public string TargetRarityColor { get; set; } = string.Empty;

    // The rarity a recipe's ten inputs must share - one rung below TargetRarityKey on the trade-up
    // ladder. Stored rather than recomputed so recipe matching never needs the ladder in SQL.
    public string TargetInputRarityKey { get; set; } = string.Empty;
    public bool TargetStatTrak { get; set; }

    // Empty means "any wear counts as a match" - the roll itself is unaffected either way.
    public List<string> TargetWears { get; set; } = [];
    public bool IsActive { get; set; } = true;

    // Each recipe holds its own outputs - the pool is not shared account-wide. A recipe with
    // capacity 1 stops firing once it has one held item, even if every other recipe is empty.
    public int HoldingCapacity { get; set; } = 1;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class CaseOpeningTradeUpRecipeObj : CaseOpeningTradeUpRecipeDbModel
{
    public int HeldCount { get; set; }
    public int EligibleInputCount { get; set; }
    public long HoldingUpgradeCost { get; set; }
}

public sealed class CaseOpeningTradeUpRecipeRequestObj
{
    public string CaseKey { get; set; } = string.Empty;
    public string SourceItemId { get; set; } = string.Empty;
    public List<string> Wears { get; set; } = [];
    public bool StatTrak { get; set; }
}

public sealed class CaseOpeningTradeUpRecipeActiveRequestObj
{
    public bool IsActive { get; set; }
}

public class CaseOpeningTradeUpHoldingDbModel : CaseOpeningHistoryObj
{
    public Guid HoldingId { get; set; }
    public Guid RecipeId { get; set; }
    public bool IsMatch { get; set; }
    public string TargetItemName { get; set; } = string.Empty;
}

public sealed class CaseOpeningTradeUpHoldingObj : CaseOpeningTradeUpHoldingDbModel
{
}

public sealed class CaseOpeningTradeUpRecipeSummaryObj
{
    public int UsedRecipeSlots { get; set; }

    // Total held items across every recipe - each recipe's own capacity lives on
    // CaseOpeningTradeUpRecipeObj.HoldingCapacity, since the pool is not shared account-wide.
    public int UsedHoldingCount { get; set; }
    public int RecipeCostStars { get; set; }
    public long RecipeCost { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long ActiveBalanceMinor { get; set; }
    public List<CaseOpeningTradeUpRecipeObj> Recipes { get; set; } = [];
    public List<CaseOpeningTradeUpHoldingObj> Holdings { get; set; } = [];
}

public class CaseOpeningBotServerDbModel
{
    public Guid ServerId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int SpeedLevel { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class CaseBattleReactionShopItemObj
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "emoji";
    public string Value { get; set; } = string.Empty;
    public int CostStars { get; set; }
    public long CostGbpPence { get; set; }
    public long Cost { get; set; }
    public bool IsOwned { get; set; }
}

public sealed class CaseBattleReactionShopObj
{
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long ActiveBalanceMinor { get; set; }
    public List<CaseBattleReactionShopItemObj> Items { get; set; } = [];
}

public sealed class CaseOpeningBotDbModel
{
    public Guid BotId { get; set; }
    public Guid ServerId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastOpenedUtc { get; set; }
    public int SpeedLevel { get; set; }
    public decimal SpeedMultiplier { get; set; }
    public int OpeningIntervalSeconds { get; set; }
    public int NextSpeedUpgradeCost { get; set; }
    public long ActiveNextSpeedUpgradeCost { get; set; }
    public bool MaximumSpeedReached { get; set; }
}

public sealed class CaseOpeningBotServerObj : CaseOpeningBotServerDbModel
{
    public List<CaseOpeningBotDbModel> Bots { get; set; } = [];
    public decimal SpeedMultiplier { get; set; }
    public int OpeningIntervalSeconds { get; set; }
    public int NextSpeedUpgradeCost { get; set; }
    public long ActiveNextSpeedUpgradeCost { get; set; }
    public bool MaximumSpeedReached { get; set; }
}

public sealed class CaseOpeningBotProgressObj
{
    public int Stars { get; set; }
    public long GbpPence { get; set; }
    public string EconomyMode { get; set; } = CaseOpeningEconomyModes.Stars;
    public long ActiveBalanceMinor { get; set; }
    public int ServerCapacity { get; set; }
    public int OpeningIntervalSeconds { get; set; }
    public int NextServerCost { get; set; }
    public int NextBotCost { get; set; }
    public long ActiveNextServerCost { get; set; }
    public long ActiveNextBotCost { get; set; }
    public int MaximumSpeedLevel { get; set; }
    public List<CaseOpeningBotServerObj> Servers { get; set; } = [];
}

public sealed class CaseOpeningBotOpenRequestObj
{
    public string CaseKey { get; set; } = string.Empty;
}

public sealed class CaseOpeningBotServerStateRequestObj
{
    public bool IsEnabled { get; set; }
}

public sealed class CaseOpeningBotCycleResultObj
{
    public List<CaseOpeningResultObj> Results { get; set; } = [];
    public bool ShouldStop { get; set; }
    public string StopReason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CaseOpeningStatisticsDbModel
{
    public long TotalOpenings { get; set; }
    public long TargetPulls { get; set; }
    public long CurrentDryStreak { get; set; }
    public DateTime? LastTargetOpenedUtc { get; set; }
}

public sealed class CaseOpeningStatisticsObj : CaseOpeningStatisticsDbModel
{
    public string CaseKey { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public string TargetRarityName { get; set; } = string.Empty;
    public decimal TargetOddsPercentage { get; set; }
    public decimal NoTargetStreakProbability { get; set; }
    public int ExpectedOpeningInterval { get; set; }
}
