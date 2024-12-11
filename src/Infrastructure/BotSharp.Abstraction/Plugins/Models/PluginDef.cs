namespace BotSharp.Abstraction.Plugins.Models;

public class PluginDef
{
    public string Id {  get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Assembly { get; set; }

    [JsonPropertyName("is_core")]
    public bool IsCore => Assembly?.StartsWith("BotSharp.Core") == true;

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("agent_ids")]
    public string[] AgentIds { get; set; } = [];

    [JsonPropertyName("settings_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SettingsName => Module?.Settings?.Name;

    public IBotSharpPlugin Module { get; set; }

    public bool Enabled { get; set; }
}
