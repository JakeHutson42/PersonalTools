using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PersonalTools.Classes.CaseOpening;
using PersonalTools.Entities.CaseOpening;

namespace PersonalTools.Data.CaseOpening;

public interface ICS2ItemPriceData
{
    Task<decimal?> GetEstimatedPrice(string marketHashName, CancellationToken cancellationToken = default);
    Task<List<CaseOpeningPriceSnapshotDbModel>> GetSnapshots(CancellationToken cancellationToken = default);
    Task<List<CaseOpeningSnapshotPriceDbModel>> GetActivePrices(CancellationToken cancellationToken = default);
    Task CreateSkinportSnapshot(IReadOnlyCollection<CaseOpeningMarketPriceTarget> targets, string? csFloatApiKey, int csFloatUsdToGbpBasisPoints, CancellationToken cancellationToken = default);
    Task ActivateSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task DeleteSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
}

/// <summary>One game item variant that must receive a snapshot price.</summary>
public sealed record CaseOpeningMarketPriceTarget(string MarketHashName, string CaseKey, string RarityKey, bool IsUltraRare, bool IsContainer = false);

/// <summary>
/// Openings only read the active immutable database snapshot. Skinport is contacted solely from
/// the explicit admin import action, so normal gameplay never depends on marketplace latency.
/// </summary>
public sealed class SkinportCS2ItemPriceData(HttpClient client, ICaseOpeningData data, ICSFloatMarketData csFloat) : ICS2ItemPriceData
{
    private readonly HttpClient _client = client;
    private readonly ICaseOpeningData _data = data;
    private readonly ICSFloatMarketData _csFloat = csFloat;
    private Task<List<CaseOpeningSnapshotPriceDbModel>>? _activePrices;

    public async Task<decimal?> GetEstimatedPrice(string marketHashName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(marketHashName)) return null;
        List<CaseOpeningSnapshotPriceDbModel> prices = await GetActivePrices(cancellationToken);
        return prices.FirstOrDefault(item => item.MarketHashName.Equals(marketHashName, StringComparison.Ordinal))?.Price;
    }

    public Task<List<CaseOpeningPriceSnapshotDbModel>> GetSnapshots(CancellationToken cancellationToken = default)
        => _data.GetCaseOpeningPriceSnapshots(cancellationToken);

    public Task<List<CaseOpeningSnapshotPriceDbModel>> GetActivePrices(CancellationToken cancellationToken = default)
        => _activePrices ??= _data.GetActiveCaseOpeningSnapshotPrices(cancellationToken);

    public async Task CreateSkinportSnapshot(IReadOnlyCollection<CaseOpeningMarketPriceTarget> targets, string? csFloatApiKey, int csFloatUsdToGbpBasisPoints, CancellationToken cancellationToken = default)
    {
        List<SkinportItem>? response = await _client.GetFromJsonAsync<List<SkinportItem>>(
            "v1/items?app_id=730&currency=GBP&tradable=0",
            cancellationToken);
        if (response is null || response.Count == 0)
        {
            throw new InvalidOperationException("Skinport returned an empty price catalogue.");
        }

        List<SkinportSalesHistoryItem> salesHistory;
        try
        {
            salesHistory = await _client.GetFromJsonAsync<List<SkinportSalesHistoryItem>>(
                "v1/sales/history?app_id=730&currency=GBP",
                cancellationToken) ?? [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            // A current-price snapshot is still useful when the separately rate-limited sales
            // endpoint is unavailable. Containers without current data remain visibly missing.
            salesHistory = [];
        }

        Dictionary<string, SkinportItem> byName = response
            .Where(item => !string.IsNullOrWhiteSpace(item.MarketHashName))
            .GroupBy(item => item.MarketHashName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, SelectBestCandidate, StringComparer.Ordinal);
        Dictionary<string, SkinportItem> byNormalisedName = response
            .Where(item => !string.IsNullOrWhiteSpace(item.MarketHashName))
            .GroupBy(item => NormaliseMarketHashName(item.MarketHashName), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, SelectBestCandidate, StringComparer.Ordinal);
        Dictionary<string, List<SkinportItem>> byBaseName = response
            .Where(item => !string.IsNullOrWhiteSpace(item.MarketHashName))
            .GroupBy(item => ParseMarketHashName(item.MarketHashName).BaseName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        Dictionary<string, SkinportSalesHistoryItem> historyByName = salesHistory
            .Where(item => !string.IsNullOrWhiteSpace(item.MarketHashName))
            .GroupBy(item => item.MarketHashName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        Dictionary<string, SkinportSalesHistoryItem> historyByNormalisedName = salesHistory
            .Where(item => !string.IsNullOrWhiteSpace(item.MarketHashName))
            .GroupBy(item => NormaliseMarketHashName(item.MarketHashName), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        List<CaseOpeningMarketPriceTarget> requiredTargets = targets
            .Where(target => !string.IsNullOrWhiteSpace(target.MarketHashName))
            .GroupBy(target => target.MarketHashName, StringComparer.Ordinal)
            .Select(group =>
            {
                CaseOpeningMarketPriceTarget first = group.First();
                return first with
                {
                    IsUltraRare = group.Any(target => target.IsUltraRare),
                    IsContainer = group.Any(target => target.IsContainer)
                };
            })
            .OrderBy(target => target.MarketHashName, StringComparer.Ordinal)
            .ToList();
        if (requiredTargets.Count == 0) throw new InvalidOperationException("No game item variants were supplied for pricing.");

        Guid snapshotId = Guid.NewGuid();
        List<CaseOpeningSnapshotPriceDbModel> prices = [];
        HashSet<string> resolvedNames = new(StringComparer.Ordinal);
        foreach (CaseOpeningMarketPriceTarget target in requiredTargets)
        {
            if (target.IsContainer && ResolveSalesHistory(target.MarketHashName, historyByName, historyByNormalisedName) is SkinportSalesHistoryItem history
                && SelectHistoricalPrice(history) is HistoricalPrice selectedHistory)
            {
                prices.Add(new CaseOpeningSnapshotPriceDbModel
                {
                    PriceSnapshotId = snapshotId,
                    MarketHashName = target.MarketHashName,
                    Price = selectedHistory.Price,
                    MinimumPrice = Positive(selectedHistory.Period.MinimumPrice),
                    MeanPrice = Positive(selectedHistory.Period.AveragePrice),
                    MedianPrice = Positive(selectedHistory.Period.MedianPrice),
                    Quantity = Math.Max(0, selectedHistory.Period.Volume),
                    IsFallback = false,
                    PriceSource = "Skinport sales",
                    PriceMethod = selectedHistory.Method,
                    SourceMarketHashName = history.MarketHashName
                });
                resolvedNames.Add(target.MarketHashName);
                continue;
            }

            (SkinportItem? item, bool isNameFallback) = ResolveMarketItem(target.MarketHashName, byName, byNormalisedName, byBaseName);
            // A knife/glove or pattern-sensitive ultra must never inherit the value of a nearby
            // wear or StatTrak listing. It needs an exact market match or its curated rule price.
            if (target.IsUltraRare && isNameFallback) continue;
            if (item is null) continue;
            (decimal? selectedPrice, string priceMethod, bool isFallback) = SelectMarketPrice(item, isNameFallback);
            if (selectedPrice is null) continue;
            prices.Add(new CaseOpeningSnapshotPriceDbModel
            {
                PriceSnapshotId = snapshotId,
                MarketHashName = target.MarketHashName,
                Price = selectedPrice.Value,
                MinimumPrice = Positive(item.MinimumPrice),
                MeanPrice = Positive(item.MeanPrice),
                MedianPrice = isNameFallback ? null : Positive(item.MedianPrice),
                SuggestedPrice = Positive(item.SuggestedPrice),
                Quantity = Math.Max(0, item.Quantity),
                SourceUpdatedUtc = item.UpdatedAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(item.UpdatedAt).UtcDateTime : null,
                IsFallback = isFallback,
                PriceSource = "Skinport",
                PriceMethod = priceMethod,
                SourceMarketHashName = item.MarketHashName
            });
            resolvedNames.Add(target.MarketHashName);
        }

        // A missing listing must not make a normal drop unpriced. Reuse the median value of
        // matched items in its own case/capsule and rarity; if that bucket is empty, use the
        // catalogue-wide rarity median. Ultra rares are deliberately excluded: they retain
        // their exact market or curated special-variant price rather than an ordinary fallback.
        Dictionary<string, CaseOpeningMarketPriceTarget> targetByName = requiredTargets.ToDictionary(target => target.MarketHashName, StringComparer.Ordinal);

        // Some stickers and older ordinary drops have real sales but no current Skinport
        // listing. Prefer their exact 30/90-day sale evidence before asking CSFloat or using a
        // broad case/rarity estimate. Ultra rares remain on their separately reviewed path.
        foreach (CaseOpeningMarketPriceTarget target in requiredTargets.Where(target =>
                     !target.IsContainer && !target.IsUltraRare && !resolvedNames.Contains(target.MarketHashName)))
        {
            SkinportSalesHistoryItem? history = ResolveSalesHistory(target.MarketHashName, historyByName, historyByNormalisedName);
            HistoricalPrice? selectedHistory = history is null ? null : SelectHistoricalPrice(history);
            if (history is null || selectedHistory is null) continue;
            prices.Add(new CaseOpeningSnapshotPriceDbModel
            {
                PriceSnapshotId = snapshotId,
                MarketHashName = target.MarketHashName,
                Price = selectedHistory.Price,
                MinimumPrice = Positive(selectedHistory.Period.MinimumPrice),
                MeanPrice = Positive(selectedHistory.Period.AveragePrice),
                MedianPrice = Positive(selectedHistory.Period.MedianPrice),
                Quantity = Math.Max(0, selectedHistory.Period.Volume),
                IsFallback = selectedHistory.Method.EndsWith("average", StringComparison.Ordinal),
                PriceSource = "Skinport sales",
                PriceMethod = selectedHistory.Method,
                SourceMarketHashName = history.MarketHashName
            });
            resolvedNames.Add(target.MarketHashName);
        }

        if (!string.IsNullOrWhiteSpace(csFloatApiKey))
        {
            CaseOpeningMarketPriceTarget[] unresolved = requiredTargets
                .Where(target => !target.IsContainer && !resolvedNames.Contains(target.MarketHashName))
                .ToArray();
            using SemaphoreSlim gate = new(4);
            Task<(CaseOpeningMarketPriceTarget Target, CSFloatMarketPrice? Price)>[] requests = unresolved.Select(async target =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return (target, await _csFloat.GetMarketPrice(target.MarketHashName, csFloatApiKey, cancellationToken));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch { return (target, null); }
                finally { gate.Release(); }
            }).ToArray();
            foreach ((CaseOpeningMarketPriceTarget target, CSFloatMarketPrice? marketPrice) in await Task.WhenAll(requests))
            {
                if (marketPrice is null) continue;
                decimal conversion = Math.Clamp(csFloatUsdToGbpBasisPoints, 1, 20_000) / 10_000m;
                prices.Add(new CaseOpeningSnapshotPriceDbModel
                {
                    PriceSnapshotId = snapshotId,
                    MarketHashName = target.MarketHashName,
                    Price = decimal.Round(marketPrice.MedianUsd * conversion, 2, MidpointRounding.AwayFromZero),
                    MinimumPrice = decimal.Round(marketPrice.MinimumUsd * conversion, 2, MidpointRounding.AwayFromZero),
                    MedianPrice = decimal.Round(marketPrice.MedianUsd * conversion, 2, MidpointRounding.AwayFromZero),
                    Quantity = marketPrice.ListingCount,
                    SourceUpdatedUtc = marketPrice.LatestListingUtc,
                    IsFallback = true,
                    PriceSource = "CSFloat",
                    PriceMethod = $"exact-listing-median-usd-to-gbp-{Math.Clamp(csFloatUsdToGbpBasisPoints, 1, 20_000)}bps",
                    SourceMarketHashName = target.MarketHashName
                });
                resolvedNames.Add(target.MarketHashName);
            }
        }

        IEnumerable<(CaseOpeningSnapshotPriceDbModel Price, CaseOpeningMarketPriceTarget Target)> normalPrices = prices
            .Where(price => !targetByName[price.MarketHashName].IsUltraRare && !targetByName[price.MarketHashName].IsContainer)
            .Select(price => (price, targetByName[price.MarketHashName]));
        Dictionary<(string CaseKey, string RarityKey), decimal> localFallbacks = normalPrices
            .GroupBy(item => (item.Target.CaseKey, item.Target.RarityKey))
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price.Price)));
        Dictionary<string, decimal> rarityFallbacks = normalPrices
            .GroupBy(item => item.Target.RarityKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price.Price)), StringComparer.OrdinalIgnoreCase);

        foreach (CaseOpeningMarketPriceTarget target in requiredTargets.Where(target => !target.IsUltraRare && !target.IsContainer && !resolvedNames.Contains(target.MarketHashName)))
        {
            bool hasLocal = localFallbacks.TryGetValue((target.CaseKey, target.RarityKey), out decimal fallback);
            bool hasRarity = !hasLocal && rarityFallbacks.TryGetValue(target.RarityKey, out fallback);
            if (!hasLocal && !hasRarity) fallback = RaritySafetyFloor(target.RarityKey);
            prices.Add(new CaseOpeningSnapshotPriceDbModel
            {
                PriceSnapshotId = snapshotId,
                MarketHashName = target.MarketHashName,
                Price = decimal.Round(fallback, 2, MidpointRounding.AwayFromZero),
                Quantity = 0,
                IsFallback = true,
                PriceSource = "Skinport",
                PriceMethod = hasLocal ? "case-rarity-median" : hasRarity ? "catalogue-rarity-median" : "safety-floor"
            });
        }

        // Standard rare-special variants use a representative Skinport median from the same
        // knife/glove family, wear band and StatTrak state when their exact listing is absent.
        // Pattern, phase, seed and exceptional-float premiums remain separate reviewed rules.
        // Build the representative pool from Skinport's whole knife/glove catalogue, not only
        // exact simulator targets. Upstream case data can describe a generic rare family while
        // the marketplace lists individual finishes, so an exact-target-only pool is often empty.
        var rareMarketPrices = response
            .Where(item => IsRareMarketItem(item.MarketHashName) && ParseMarketHashName(item.MarketHashName).WearIndex is not null)
            .Select(item => new
            {
                Item = item,
                Price = SelectMarketPrice(item, false).Price,
                Family = RareFamily(item.MarketHashName),
                Wear = ParseMarketHashName(item.MarketHashName).WearIndex,
                StatTrak = IsStatTrak(item.MarketHashName)
            })
            .Where(item => item.Price is > 0)
            .ToList();
        Dictionary<(string Family, int? Wear, bool StatTrak), decimal> rareFallbacks = rareMarketPrices
            .GroupBy(item => (item.Family, item.Wear, item.StatTrak))
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price!.Value)));
        Dictionary<(string Family, int? Wear), decimal> broadRareFallbacks = rareMarketPrices
            .GroupBy(item => (item.Family, item.Wear))
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price!.Value)));

        foreach (CaseOpeningMarketPriceTarget target in requiredTargets.Where(target => target.IsUltraRare && !resolvedNames.Contains(target.MarketHashName)))
        {
            MarketHashParts parsed = ParseMarketHashName(target.MarketHashName);
            string family = RareFamily(target.MarketHashName);
            decimal representative = rareFallbacks.GetValueOrDefault((family, parsed.WearIndex, IsStatTrak(target.MarketHashName)),
                broadRareFallbacks.GetValueOrDefault((family, parsed.WearIndex), 0m));
            if (representative <= 0) continue;
            prices.Add(new CaseOpeningSnapshotPriceDbModel
            {
                PriceSnapshotId = snapshotId,
                MarketHashName = target.MarketHashName,
                Price = decimal.Round(representative, 2, MidpointRounding.AwayFromZero),
                Quantity = 0,
                IsFallback = true,
                PriceSource = "Skinport",
                PriceMethod = "representative-rare-market-family-wear-median",
                SourceMarketHashName = $"Skinport {family} market median"
            });
            resolvedNames.Add(target.MarketHashName);
        }

        if (prices.Count == 0)
        {
            throw new InvalidOperationException("None of the simulator items matched Skinport market names. The snapshot was not created.");
        }

        DateTime now = DateTime.UtcNow;
        await _data.CreateCaseOpeningPriceSnapshot(new CaseOpeningPriceSnapshotDbModel
        {
            PriceSnapshotId = snapshotId,
            Name = $"Skinport GBP · {now:yyyy-MM-dd HH:mm} UTC",
            Source = salesHistory.Count > 0 ? "Skinport current + sales history" : "Skinport current",
            Currency = "GBP",
            PriceBasis = salesHistory.Count > 0
                ? "Skinport variants, 30/90-day container medians and rare-family medians"
                : "Skinport wear variants, current container prices and representative rare medians",
            SourceItemCount = Math.Max(response.Count, salesHistory.Count),
            MatchedItemCount = prices.Count,
            IsActive = true,
            ImportedUtc = now
        }, prices, cancellationToken);
        _activePrices = null;
    }

    public async Task ActivateSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _data.ActivateCaseOpeningPriceSnapshot(snapshotId, cancellationToken);
        _activePrices = null;
    }

    public async Task DeleteSnapshot(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        await _data.DeleteCaseOpeningPriceSnapshot(snapshotId, cancellationToken);
        _activePrices = null;
    }

    private static decimal? Positive(decimal? value) => value is > 0 ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;

    private static SkinportSalesHistoryItem? ResolveSalesHistory(
        string marketHashName,
        IReadOnlyDictionary<string, SkinportSalesHistoryItem> byName,
        IReadOnlyDictionary<string, SkinportSalesHistoryItem> byNormalisedName)
    {
        if (byName.TryGetValue(marketHashName, out SkinportSalesHistoryItem? exact)) return exact;
        byNormalisedName.TryGetValue(NormaliseMarketHashName(marketHashName), out SkinportSalesHistoryItem? normalised);
        return normalised;
    }

    private static HistoricalPrice? SelectHistoricalPrice(SkinportSalesHistoryItem item)
    {
        if (HistoricalMedian(item.Last30Days, 3) is decimal thirtyDayMedian)
            return new HistoricalPrice(thirtyDayMedian, item.Last30Days!, "exact-sales-30d-median");
        if (HistoricalMedian(item.Last90Days, 1) is decimal ninetyDayMedian)
            return new HistoricalPrice(ninetyDayMedian, item.Last90Days!, "exact-sales-90d-median");
        if (HistoricalAverage(item.Last30Days, 3) is decimal thirtyDayAverage)
            return new HistoricalPrice(thirtyDayAverage, item.Last30Days!, "exact-sales-30d-average");
        if (HistoricalAverage(item.Last90Days, 1) is decimal ninetyDayAverage)
            return new HistoricalPrice(ninetyDayAverage, item.Last90Days!, "exact-sales-90d-average");
        return null;
    }

    private static decimal? HistoricalMedian(SkinportSalesPeriod? period, int minimumVolume)
        => period is { Volume: >= 1 } && period.Volume >= minimumVolume ? Positive(period.MedianPrice) : null;

    private static decimal? HistoricalAverage(SkinportSalesPeriod? period, int minimumVolume)
        => period is { Volume: >= 1 } && period.Volume >= minimumVolume ? Positive(period.AveragePrice) : null;

    private static (decimal? Price, string Method, bool IsFallback) SelectMarketPrice(SkinportItem item, bool isNameFallback)
    {
        if (Positive(item.MedianPrice) is decimal median) return (median, isNameFallback ? "near-name-median" : "exact-median", isNameFallback);
        if (Positive(item.SuggestedPrice) is decimal suggested) return (suggested, isNameFallback ? "near-name-suggested" : "exact-suggested", true);
        if (Positive(item.MeanPrice) is decimal mean) return (mean, isNameFallback ? "near-name-mean" : "exact-mean", true);
        return (Positive(item.MinimumPrice), isNameFallback ? "near-name-minimum" : "exact-minimum", true);
    }

    private static decimal Median(IEnumerable<decimal> values)
    {
        decimal[] ordered = values.OrderBy(value => value).ToArray();
        int middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2m : ordered[middle];
    }

    private static decimal RaritySafetyFloor(string rarityKey) => rarityKey.ToLowerInvariant() switch
    {
        "mil-spec" or "high-grade" => .10m,
        "restricted" or "remarkable" => .35m,
        "classified" or "exotic" => 1.00m,
        "covert" => 3.00m,
        _ => .10m
    };

    private static (SkinportItem? Item, bool IsNameFallback) ResolveMarketItem(
        string marketHashName,
        IReadOnlyDictionary<string, SkinportItem> byName,
        IReadOnlyDictionary<string, SkinportItem> byNormalisedName,
        IReadOnlyDictionary<string, List<SkinportItem>> byBaseName)
    {
        if (byName.TryGetValue(marketHashName, out SkinportItem? exact)) return (exact, false);
        if (byNormalisedName.TryGetValue(NormaliseMarketHashName(marketHashName), out SkinportItem? normalised)) return (normalised, false);

        MarketHashParts wanted = ParseMarketHashName(marketHashName);
        if (!byBaseName.TryGetValue(wanted.BaseName, out List<SkinportItem>? candidates) || candidates.Count == 0) return (null, false);

        // Use the same skin/wear without StatTrak first. If it is unavailable, use only the
        // closest available wear for that exact base skin—never a different weapon or finish.
        List<SkinportItem> sameWear = candidates.Where(candidate => ParseMarketHashName(candidate.MarketHashName).WearIndex == wanted.WearIndex).ToList();
        SkinportItem item = (sameWear.Count > 0 ? sameWear : candidates)
            .OrderBy(candidate => WearDistance(ParseMarketHashName(candidate.MarketHashName).WearIndex, wanted.WearIndex))
            .ThenByDescending(candidate => candidate.MedianPrice is > 0)
            .ThenByDescending(candidate => candidate.Quantity)
            .First();
        return (item, true);
    }

    private static SkinportItem SelectBestCandidate(IEnumerable<SkinportItem> candidates)
        => candidates.OrderByDescending(item => item.MedianPrice is > 0).ThenByDescending(item => item.Quantity).First();

    private static string NormaliseMarketHashName(string value)
        => string.Join(' ', value.Trim()
            .Replace("™", string.Empty, StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static MarketHashParts ParseMarketHashName(string value)
    {
        string normalised = NormaliseMarketHashName(value);
        int wearStart = normalised.LastIndexOf(" (", StringComparison.Ordinal);
        if (wearStart < 0 || !normalised.EndsWith(')')) return new MarketHashParts(RemoveStatTrakPrefix(normalised), null);
        string wear = normalised[(wearStart + 2)..^1];
        return new MarketHashParts(RemoveStatTrakPrefix(normalised[..wearStart]), WearIndex(wear));
    }

    private static string RemoveStatTrakPrefix(string value)
        => value.Replace("★ STATTRAK ", "★ ", StringComparison.Ordinal).Replace("STATTRAK ", string.Empty, StringComparison.Ordinal);

    private static int? WearIndex(string wear) => wear switch
    {
        "FACTORY NEW" => 0, "MINIMAL WEAR" => 1, "FIELD-TESTED" => 2, "WELL-WORN" => 3, "BATTLE-SCARRED" => 4, _ => null
    };

    private static int WearDistance(int? left, int? right) => left is null || right is null ? 99 : Math.Abs(left.Value - right.Value);

    private static bool IsStatTrak(string value) => NormaliseMarketHashName(value).Contains("STATTRAK", StringComparison.Ordinal);

    private static bool IsRareMarketItem(string value)
        => NormaliseMarketHashName(value).StartsWith('★');

    private static string RareFamily(string value)
    {
        string normalised = NormaliseMarketHashName(value);
        return normalised.Contains("GLOVES", StringComparison.Ordinal) || normalised.Contains("HAND WRAPS", StringComparison.Ordinal)
            ? "glove"
            : "knife";
    }

    private sealed record MarketHashParts(string BaseName, int? WearIndex);
    private sealed record HistoricalPrice(decimal Price, SkinportSalesPeriod Period, string Method);

    private sealed class SkinportSalesHistoryItem
    {
        [JsonPropertyName("market_hash_name")] public string MarketHashName { get; set; } = string.Empty;
        [JsonPropertyName("last_30_days")] public SkinportSalesPeriod? Last30Days { get; set; }
        [JsonPropertyName("last_90_days")] public SkinportSalesPeriod? Last90Days { get; set; }
    }

    private sealed class SkinportSalesPeriod
    {
        [JsonPropertyName("min")] public decimal? MinimumPrice { get; set; }
        [JsonPropertyName("avg")] public decimal? AveragePrice { get; set; }
        [JsonPropertyName("median")] public decimal? MedianPrice { get; set; }
        [JsonPropertyName("volume")] public int Volume { get; set; }
    }

    private sealed class SkinportItem
    {
        [JsonPropertyName("market_hash_name")] public string MarketHashName { get; set; } = string.Empty;
        [JsonPropertyName("suggested_price")] public decimal? SuggestedPrice { get; set; }
        [JsonPropertyName("min_price")] public decimal? MinimumPrice { get; set; }
        [JsonPropertyName("mean_price")] public decimal? MeanPrice { get; set; }
        [JsonPropertyName("median_price")] public decimal? MedianPrice { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("updated_at")] public long UpdatedAt { get; set; }
    }
}
