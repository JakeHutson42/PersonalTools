using System.Text.Json.Serialization;

namespace PersonalTools.Entities.Skins
{
    public class Cs2ApiNamedObj
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}