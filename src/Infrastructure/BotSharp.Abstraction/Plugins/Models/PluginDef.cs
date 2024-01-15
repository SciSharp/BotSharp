namespace BotSharp.Abstraction.Plugins.Models;

public class PluginDef
{
    public string Id {  get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Assembly { get; set; }
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("agent_ids")]
    public string[] AgentIds { get; set; } = new string[0];

    [JsonPropertyName("settings_name")]
    public string SettingsName => Module?.Settings?.Name;
    public IBotSharpPlugin Module { get; set; }

    public bool Enabled { get; set; }

    public PluginMenuDef[] Menus { get; set; }
}
