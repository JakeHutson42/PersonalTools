using System.Text.Json.Serialization;

namespace PersonalTools.Entities.Skins
{
    public class Cs2ApiSkinObj
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        [JsonPropertyName("weapon")]
        public Cs2ApiNamedObj? Weapon { get; set; }

        [JsonPropertyName("wear")]
        public Cs2ApiNamedObj? Wear { get; set; }
    }
}