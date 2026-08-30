namespace PersonalTools.Classes.CaseOpening;

public static class CaseOpeningEconomyPolicy
{
    public static long SelectActiveAmount(string economyMode, long stars, long gbpPence)
        => string.Equals(economyMode, "gbp", StringComparison.OrdinalIgnoreCase) ? gbpPence : stars;

    public static bool HasSufficientBalance(string economyMode, long starsBalance, long gbpPenceBalance, long starsCost, long gbpPenceCost)
        => SelectActiveAmount(economyMode, starsBalance, gbpPenceBalance) >= SelectActiveAmount(economyMode, starsCost, gbpPenceCost);

    public static int RemainingFreeCases(bool enabled, int quantity, int claimed, DateTime windowStartedUtc, int windowHours, DateTime nowUtc)
        => !enabled ? 0 : windowStartedUtc <= nowUtc.AddHours(-Math.Max(1, windowHours)) ? Math.Max(0, quantity) : Math.Max(0, quantity - claimed);

    public static (decimal? Price, bool IsFallback) SelectMarketPrice(decimal? medianPrice, decimal? suggestedPrice, decimal? meanPrice = null, decimal? minimumPrice = null)
    {
        decimal? median = Positive(medianPrice);
        if (median is not null) return (median, false);
        decimal? suggested = Positive(suggestedPrice);
        if (suggested is not null) return (suggested, true);
        decimal? mean = Positive(meanPrice);
        if (mean is not null) return (mean, true);
        return (Positive(minimumPrice), true);
    }

    public static List<string> ValidateTierCoverage(IEnumerable<int> tiers)
    {
        int[] values = tiers.ToArray();
        return Enumerable.Range(1, 10)
            .Select(tier => new { Tier = tier, Count = values.Count(value => value == tier) })
            .Where(item => item.Count < 2)
            .Select(item => $"Tier {item.Tier} has {item.Count} case{(item.Count == 1 ? string.Empty : "s")}; at least two are required.")
            .ToList();
    }

    public static bool IsBlockingInferredPriceMethod(string? method)
    {
        string value = method ?? string.Empty;
        return value.Contains("catalogue-rarity-median", StringComparison.OrdinalIgnoreCase)
            || value.Contains("safety-floor", StringComparison.OrdinalIgnoreCase)
            || value.Contains("legacy-inferred", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal? Positive(decimal? value) => value is > 0 ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;
}
