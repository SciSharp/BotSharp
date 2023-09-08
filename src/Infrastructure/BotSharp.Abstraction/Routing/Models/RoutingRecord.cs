using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingRecord
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public List<string> RequiredFields { get; set; } = new List<string>();

    [JsonPropertyName("redirect_to")]
    public string? RedirectTo { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
