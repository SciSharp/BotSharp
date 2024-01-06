namespace BotSharp.Abstraction.Plugins.Models;

public class PluginDef
{
    public string Id {  get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Assembly { get; set; }
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("with_agent")]
    public bool WithAgent { get; set; }
}
