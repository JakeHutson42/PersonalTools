using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PersonalTools.Entities.CaseOpening;

namespace PersonalTools.Data.CaseOpening;

public interface ICSFloatMarketData
{
    Task<List<CaseOpeningSpecialVariantListingEvidenceObj>> GetListings(CaseOpeningSpecialVariantRuleDbModel rule, string apiKey, CancellationToken cancellationToken = default);
}

public sealed class CSFloatMarketData(HttpClient client) : ICSFloatMarketData
{
    public async Task<List<CaseOpeningSpecialVariantListingEvidenceObj>> GetListings(CaseOpeningSpecialVariantRuleDbModel rule, string apiKey, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { "limit=50", "sort_by=lowest_price", $"market_hash_name={Uri.EscapeDataString(rule.MarketHashName)}" };
        if (rule.PatternSeed is int seed) query.Add($"paint_seed={seed}");
        if (rule.MinimumFloat is decimal minimum) query.Add($"min_float={minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (rule.MaximumFloat is decimal maximum) query.Add($"max_float={maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (rule.RequiresStatTrak is bool statTrak) query.Add($"category={(statTrak ? 2 : 1)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/listings?{string.Join('&', query)}");
        request.Headers.TryAddWithoutValidation("Authorization", apiKey);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"CSFloat listing search failed ({(int)response.StatusCode}). Check the saved API key and try again later.");
        List<Listing>? listings = await response.Content.ReadFromJsonAsync<List<Listing>>(cancellationToken);
        return (listings ?? []).Where(item => item.Item is not null && item.Price >= 0).Select(item => new CaseOpeningSpecialVariantListingEvidenceObj { RuleId = rule.RuleId, RuleName = rule.Name, ListingId = item.Id, PriceMinor = item.Price / 100m, FloatValue = item.Item!.FloatValue, PatternSeed = item.Item.PaintSeed, CreatedUtc = item.CreatedUtc }).ToList();
    }
    private sealed class Listing { public string Id { get; set; } = string.Empty; [JsonPropertyName("price")] public long Price { get; set; } [JsonPropertyName("created_at")] public DateTime CreatedUtc { get; set; } public Item? Item { get; set; } }
    private sealed class Item { [JsonPropertyName("float_value")] public decimal FloatValue { get; set; } [JsonPropertyName("paint_seed")] public int PaintSeed { get; set; } }
}
