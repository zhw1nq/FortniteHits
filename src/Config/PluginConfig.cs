using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace FortniteHits.Config;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("distance")]
    public float Distance { get; set; } = 40.0f;

    [JsonPropertyName("free_access")]
    public bool FreeAccess { get; set; } = true;

    [JsonPropertyName("commands")]
    public List<string> Commands { get; set; } = new() { "fortnite", "fn", "damage" };

    [JsonPropertyName("commands_enabled")]
    public bool CommandsEnabled { get; set; } = true;

    [JsonPropertyName("is_debug")]
    public bool IsDebug { get; set; } = false;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 3;
}