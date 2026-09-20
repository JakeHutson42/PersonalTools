namespace PersonalTools.Classes.CaseOpening;

/// <summary>
/// The single source of truth for the case-economy curve. Market prices are stored in GBP,
/// while Stars deliberately use the same whole-unit value: £100 is worth 100 Stars.
/// Keep prestige modifiers out of these base prices; they can be applied as a multiplier at
/// purchase/reward time without rebaking every case when the prestige system is introduced.
/// </summary>
public static class CaseOpeningBalancePolicy
{
    private const decimal PencePerGbp = 100m;
    public static CaseOpeningBalanceRecommendation RecommendCasePrices(decimal expectedMarketValueGbp, int tier, int skinSaleRateBasisPoints, int targetProfitBasisPoints, int globalReturnMultiplierBasisPoints, int priceRoundingPence, bool isStarterCase)
    {
        int resolvedTier = Math.Clamp(tier, 1, 10);
        decimal expectedSaleValuePence = decimal.Round(expectedMarketValueGbp * PencePerGbp * Math.Clamp(skinSaleRateBasisPoints, 0, 10_000) / 10_000m, 2, MidpointRounding.AwayFromZero);

        if (isStarterCase)
            return new CaseOpeningBalanceRecommendation(expectedSaleValuePence, 0m, 0, 0, 0, 0);

        decimal baseReturnBasisPoints = 10_000m + Math.Clamp(targetProfitBasisPoints, 0, 20_000);
        decimal targetReturnPercentage = baseReturnBasisPoints * Math.Clamp(globalReturnMultiplierBasisPoints, 5_000, 20_000) / 1_000_000m;
        decimal purchasePence = expectedSaleValuePence / (targetReturnPercentage / 100m);
        long recommendedPurchasePence = RoundPence(purchasePence, priceRoundingPence);
        int recommendedPurchaseStars = PenceToStars(recommendedPurchasePence);
        long recommendedUnlockPence = RoundPence(recommendedPurchasePence * (8m + resolvedTier), priceRoundingPence);
        int recommendedUnlockStars = PenceToStars(recommendedUnlockPence);

        return new CaseOpeningBalanceRecommendation(expectedSaleValuePence, targetReturnPercentage, recommendedPurchasePence, recommendedUnlockPence, recommendedPurchaseStars, recommendedUnlockStars);
    }

    public static int PenceToStars(decimal pence)
    {
        return Math.Max(1, decimal.ToInt32(decimal.Round(pence / PencePerGbp, 0, MidpointRounding.AwayFromZero)));
    }

    public static long StarsToPence(int stars) => Math.Max(0, (long)stars * 100L);

    public static CaseOpeningSaleAward CalculateSaleAward(decimal? marketValueGbp, int skinSaleRateBasisPoints, int fallbackStars)
    {
        // A snapshot price is authoritative. The rarity value is retained only as a safe
        // fallback while an old inventory item has no matching snapshot price.
        decimal grossPence = marketValueGbp is decimal value ? value * PencePerGbp : Math.Max(0, fallbackStars) * PencePerGbp;
        long awardedPence = Math.Max(0, (long)decimal.Round(grossPence * Math.Clamp(skinSaleRateBasisPoints, 0, 10_000) / 10_000m, 0, MidpointRounding.AwayFromZero));
        int awardedStars = awardedPence == 0 ? 0 : PenceToStars(awardedPence);

        return new CaseOpeningSaleAward(awardedStars, awardedPence);
    }

    private static long RoundPence(decimal pence, int configuredIncrement)
    {
        int increment = Math.Clamp(configuredIncrement, 1, 10_000);
        return Math.Max(increment, (long)(Math.Round(pence / increment, MidpointRounding.AwayFromZero) * increment));
    }
}

public sealed record CaseOpeningBalanceRecommendation(decimal ExpectedSaleValuePence, decimal TargetReturnPercentage, long RecommendedPurchaseGbpPence, long RecommendedUnlockGbpPence, int RecommendedPurchaseStars, int RecommendedUnlockStars);

public sealed record CaseOpeningSaleAward(int Stars, long GbpPence);
