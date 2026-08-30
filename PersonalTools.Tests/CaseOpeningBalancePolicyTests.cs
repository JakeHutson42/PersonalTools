using PersonalTools.Classes.CaseOpening;
using PersonalTools.Data.CaseOpening;

namespace PersonalTools.Tests;

public sealed class CaseOpeningBalancePolicyTests
{
    [Fact]
    public void CuratedCatalogue_HasExpectedCoverageAndStableUniqueMappings()
    {
        Assert.Equal(82, CaseOpeningCatalogue.Containers.Count);
        Assert.Equal(82, CaseOpeningCatalogue.Containers.Keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(82, CaseOpeningCatalogue.Containers.Values.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("crate-4904", CaseOpeningCatalogue.Containers["kilowatt"]);
    }

    [Theory]
    [InlineData(1, 105.06)]
    [InlineData(5, 113.30)]
    [InlineData(10, 123.60)]
    public void RecommendCasePrices_AppliesTierProfitAndGlobalMultiplier(int tier, double expectedReturn)
    {
        CaseOpeningBalanceRecommendation result = CaseOpeningBalancePolicy.RecommendCasePrices(
            expectedMarketValueGbp: 100m,
            tier,
            skinSaleRateBasisPoints: 10_000,
            targetProfitBasisPoints: tier * 200,
            globalReturnMultiplierBasisPoints: 10_300,
            priceRoundingPence: 1,
            isStarterCase: false);

        Assert.Equal((decimal)expectedReturn, result.TargetReturnPercentage);
        Assert.True(result.RecommendedPurchaseGbpPence > 0);
        Assert.True(result.RecommendedUnlockGbpPence > result.RecommendedPurchaseGbpPence);
    }

    [Fact]
    public void DefaultProgressionCurve_IsMonotonicAndWithinRoundingTolerance()
    {
        decimal previousTarget = 0m;
        long previousPurchase = 0;

        for (int tier = 1; tier <= 10; tier++)
        {
            decimal expectedValue = tier * 25m;
            int rounding = tier switch { <= 1 => 1, <= 4 => 5, <= 6 => 10, <= 8 => 25, 9 => 50, _ => 100 };
            CaseOpeningBalanceRecommendation result = CaseOpeningBalancePolicy.RecommendCasePrices(
                expectedValue, tier, 10_000, tier * 200, 10_300, rounding, false);

            decimal realisedReturn = result.ExpectedSaleValuePence / result.RecommendedPurchaseGbpPence * 100m;
            decimal maximumRoundingDrift = (decimal)rounding / result.RecommendedPurchaseGbpPence * 100m;

            Assert.True(result.TargetReturnPercentage > previousTarget);
            Assert.True(result.RecommendedPurchaseGbpPence > previousPurchase);
            Assert.InRange(Math.Abs(realisedReturn - result.TargetReturnPercentage), 0m, maximumRoundingDrift + 0.01m);
            Assert.Equal(result.RecommendedPurchaseStars, CaseOpeningBalancePolicy.PenceToStars(result.RecommendedPurchaseGbpPence));

            previousTarget = result.TargetReturnPercentage;
            previousPurchase = result.RecommendedPurchaseGbpPence;
        }
    }

    [Fact]
    public void StarterCase_RemainsFreeWithoutChangingExpectedSaleValue()
    {
        CaseOpeningBalanceRecommendation result = CaseOpeningBalancePolicy.RecommendCasePrices(
            12.50m, 1, 8_500, 200, 10_300, 1, true);

        Assert.Equal(1062.50m, result.ExpectedSaleValuePence);
        Assert.Equal(0m, result.TargetReturnPercentage);
        Assert.Equal(0, result.RecommendedPurchaseGbpPence);
        Assert.Equal(0, result.RecommendedUnlockGbpPence);
        Assert.Equal(0, result.RecommendedPurchaseStars);
        Assert.Equal(0, result.RecommendedUnlockStars);
    }

    [Fact]
    public void SaleAward_UsesMarketPriceAndPreservesCurrencyParity()
    {
        CaseOpeningSaleAward result = CaseOpeningBalancePolicy.CalculateSaleAward(24.75m, 8_000, fallbackStars: 999);

        Assert.Equal(1_980, result.GbpPence);
        Assert.Equal(20, result.Stars);
        Assert.Equal(2_000, CaseOpeningBalancePolicy.StarsToPence(result.Stars));
    }

    [Fact]
    public void LongRunSimulation_ConvergesOnConfiguredExpectedReturn()
    {
        // This deterministic distribution represents common, uncommon and jackpot outcomes.
        // Its weighted mean is fed through the production pricing and sale-award policies.
        (decimal Value, int Weight)[] outcomes = [(0.50m, 8_000), (5m, 1_800), (100m, 200)];
        decimal expectedMarketValue = outcomes.Sum(item => item.Value * item.Weight) / outcomes.Sum(item => item.Weight);
        CaseOpeningBalanceRecommendation recommendation = CaseOpeningBalancePolicy.RecommendCasePrices(
            expectedMarketValue, 6, 8_500, 1_200, 10_300, 10, false);

        Random random = new(20_260_830);
        long totalAwards = 0;
        int totalWeight = outcomes.Sum(item => item.Weight);
        const int openings = 2_000_000;
        for (int index = 0; index < openings; index++)
        {
            int roll = random.Next(totalWeight);
            int cumulative = 0;
            decimal selectedValue = outcomes[^1].Value;
            foreach ((decimal value, int weight) in outcomes)
            {
                cumulative += weight;
                if (roll >= cumulative) continue;
                selectedValue = value;
                break;
            }

            totalAwards += CaseOpeningBalancePolicy.CalculateSaleAward(selectedValue, 8_500, 0).GbpPence;
        }

        decimal simulatedReturn = totalAwards / (decimal)(recommendation.RecommendedPurchaseGbpPence * openings) * 100m;
        Assert.InRange(simulatedReturn, recommendation.TargetReturnPercentage - 1.5m, recommendation.TargetReturnPercentage + 1.5m);
    }

    [Fact]
    public void BalanceInputs_AreClampedAtSafeLimits()
    {
        CaseOpeningBalanceRecommendation result = CaseOpeningBalancePolicy.RecommendCasePrices(
            10m, tier: 99, skinSaleRateBasisPoints: 99_999, targetProfitBasisPoints: 99_999,
            globalReturnMultiplierBasisPoints: 99_999, priceRoundingPence: 0, isStarterCase: false);

        Assert.Equal(600m, result.TargetReturnPercentage);
        Assert.Equal(1_000m, result.ExpectedSaleValuePence);
        Assert.True(result.RecommendedPurchaseGbpPence > 0);
    }
}
