using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace FortniteHits.Config;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("distance")]
    public float Distance { get; set; } = 40.0f;

    [JsonPropertyName("free_access")]
    public bool FreeAccess { get; set; } = true;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("commands")]
    public List<string> Commands { get; set; } = new() { "fortnite", "fn", "damage" };

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;
}