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
    Task CreateSkinportSnapshot(IReadOnlyCollection<CaseOpeningMarketPriceTarget> targets, CancellationToken cancellationToken = default);
    Task ActivateSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
    Task DeleteSnapshot(Guid snapshotId, CancellationToken cancellationToken = default);
}

/// <summary>One game item variant that must receive a snapshot price.</summary>
public sealed record CaseOpeningMarketPriceTarget(string MarketHashName, string CaseKey, string RarityKey, bool IsUltraRare);

/// <summary>
/// Openings only read the active immutable database snapshot. Skinport is contacted solely from
/// the explicit admin import action, so normal gameplay never depends on marketplace latency.
/// </summary>
public sealed class SkinportCS2ItemPriceData(HttpClient client, ICaseOpeningData data) : ICS2ItemPriceData
{
    private readonly HttpClient _client = client;
    private readonly ICaseOpeningData _data = data;
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

    public async Task CreateSkinportSnapshot(IReadOnlyCollection<CaseOpeningMarketPriceTarget> targets, CancellationToken cancellationToken = default)
    {
        List<SkinportItem>? response = await _client.GetFromJsonAsync<List<SkinportItem>>(
            "v1/items?app_id=730&currency=GBP&tradable=0",
            cancellationToken);
        if (response is null || response.Count == 0)
        {
            throw new InvalidOperationException("Skinport returned an empty price catalogue.");
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

        List<CaseOpeningMarketPriceTarget> requiredTargets = targets
            .Where(target => !string.IsNullOrWhiteSpace(target.MarketHashName))
            .GroupBy(target => target.MarketHashName, StringComparer.Ordinal)
            .Select(group =>
            {
                CaseOpeningMarketPriceTarget first = group.First();
                return first with { IsUltraRare = group.Any(target => target.IsUltraRare) };
            })
            .OrderBy(target => target.MarketHashName, StringComparer.Ordinal)
            .ToList();
        if (requiredTargets.Count == 0) throw new InvalidOperationException("No game item variants were supplied for pricing.");

        Guid snapshotId = Guid.NewGuid();
        List<CaseOpeningSnapshotPriceDbModel> prices = [];
        HashSet<string> resolvedNames = new(StringComparer.Ordinal);
        foreach (CaseOpeningMarketPriceTarget target in requiredTargets)
        {
            (SkinportItem? item, bool isNameFallback) = ResolveMarketItem(target.MarketHashName, byName, byNormalisedName, byBaseName);
            // A knife/glove or pattern-sensitive ultra must never inherit the value of a nearby
            // wear or StatTrak listing. It needs an exact market match or its curated rule price.
            if (target.IsUltraRare && isNameFallback) continue;
            if (item is null) continue;
            (decimal? selectedPrice, bool isFallback) = CaseOpeningEconomyPolicy.SelectMarketPrice(item.MedianPrice, item.SuggestedPrice, item.MeanPrice, item.MinimumPrice);
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
                IsFallback = isFallback || isNameFallback
            });
            resolvedNames.Add(target.MarketHashName);
        }

        // A missing listing must not make a normal drop unpriced. Reuse the median value of
        // matched items in its own case/capsule and rarity; if that bucket is empty, use the
        // catalogue-wide rarity median. Ultra rares are deliberately excluded: they retain
        // their exact market or curated special-variant price rather than an ordinary fallback.
        Dictionary<string, CaseOpeningMarketPriceTarget> targetByName = requiredTargets.ToDictionary(target => target.MarketHashName, StringComparer.Ordinal);
        IEnumerable<(CaseOpeningSnapshotPriceDbModel Price, CaseOpeningMarketPriceTarget Target)> normalPrices = prices
            .Where(price => !targetByName[price.MarketHashName].IsUltraRare)
            .Select(price => (price, targetByName[price.MarketHashName]));
        Dictionary<(string CaseKey, string RarityKey), decimal> localFallbacks = normalPrices
            .GroupBy(item => (item.Target.CaseKey, item.Target.RarityKey))
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price.Price)));
        Dictionary<string, decimal> rarityFallbacks = normalPrices
            .GroupBy(item => item.Target.RarityKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => Median(group.Select(item => item.Price.Price)), StringComparer.OrdinalIgnoreCase);

        foreach (CaseOpeningMarketPriceTarget target in requiredTargets.Where(target => !target.IsUltraRare && !resolvedNames.Contains(target.MarketHashName)))
        {
            decimal fallback = localFallbacks.GetValueOrDefault((target.CaseKey, target.RarityKey),
                rarityFallbacks.GetValueOrDefault(target.RarityKey, RaritySafetyFloor(target.RarityKey)));
            prices.Add(new CaseOpeningSnapshotPriceDbModel
            {
                PriceSnapshotId = snapshotId,
                MarketHashName = target.MarketHashName,
                Price = decimal.Round(fallback, 2, MidpointRounding.AwayFromZero),
                Quantity = 0,
                IsFallback = true
            });
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
            Source = "Skinport",
            Currency = "GBP",
            PriceBasis = "median, suggested/mean/minimum, matching-name, case-rarity fallback",
            SourceItemCount = response.Count,
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
        => string.Join(' ', value.Trim().Replace("™", string.Empty, StringComparison.Ordinal).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

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

    private sealed record MarketHashParts(string BaseName, int? WearIndex);

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
