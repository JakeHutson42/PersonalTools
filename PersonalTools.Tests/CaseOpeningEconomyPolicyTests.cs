using PersonalTools.Classes.CaseOpening;

namespace PersonalTools.Tests;

public sealed class CaseOpeningEconomyPolicyTests
{
    [Theory]
    [InlineData("stars", 100, 2500, 100)]
    [InlineData("gbp", 100, 2500, 2500)]
    public void SelectActiveAmount_UsesConfiguredEconomy(string mode, long stars, long gbp, long expected)
    {
        Assert.Equal(expected, CaseOpeningEconomyPolicy.SelectActiveAmount(mode, stars, gbp));
    }

    [Fact]
    public void HasSufficientBalance_DoesNotUseInactiveCurrency()
    {
        Assert.False(CaseOpeningEconomyPolicy.HasSufficientBalance("gbp", 10_000, 99, 5, 100));
        Assert.True(CaseOpeningEconomyPolicy.HasSufficientBalance("stars", 10_000, 0, 5, 100_000));
    }

    [Fact]
    public void RemainingFreeCases_ReplenishesAfterWindow()
    {
        DateTime now = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(7, CaseOpeningEconomyPolicy.RemainingFreeCases(true, 10, 3, now.AddHours(-1), 24, now));
        Assert.Equal(10, CaseOpeningEconomyPolicy.RemainingFreeCases(true, 10, 10, now.AddHours(-24), 24, now));
        Assert.Equal(0, CaseOpeningEconomyPolicy.RemainingFreeCases(false, 10, 0, now, 24, now));
    }

    [Fact]
    public void SelectMarketPrice_PrefersCurrentMedian()
    {
        (decimal? price, bool fallback) = CaseOpeningEconomyPolicy.SelectMarketPrice(12.345m, 10m);

        Assert.Equal(12.35m, price);
        Assert.False(fallback);
    }

    [Fact]
    public void SelectMarketPrice_MarksSuggestedPriceAsFallback()
    {
        (decimal? price, bool fallback) = CaseOpeningEconomyPolicy.SelectMarketPrice(null, 10.126m);

        Assert.Equal(10.13m, price);
        Assert.True(fallback);
    }

    [Fact]
    public void SelectMarketPrice_RejectsNonPositiveValues()
    {
        (decimal? price, bool fallback) = CaseOpeningEconomyPolicy.SelectMarketPrice(0m, -1m);

        Assert.Null(price);
        Assert.True(fallback);
    }

    [Fact]
    public void ValidateTierCoverage_AcceptsTwoCasesInEveryTier()
    {
        IEnumerable<int> tiers = Enumerable.Range(1, 10).SelectMany(tier => new[] { tier, tier });

        Assert.Empty(CaseOpeningEconomyPolicy.ValidateTierCoverage(tiers));
    }

    [Fact]
    public void ValidateTierCoverage_ReportsSparseAndMissingTiers()
    {
        List<string> warnings = CaseOpeningEconomyPolicy.ValidateTierCoverage(new[] { 1, 2, 2 });

        Assert.Contains("Tier 1 has 1 case; at least two are required.", warnings);
        Assert.Contains("Tier 3 has 0 cases; at least two are required.", warnings);
    }
}
