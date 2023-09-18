namespace BotSharp.Abstraction.Routing.Models;

public class RoutingItem
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public string[] RequiredFields { get; set; } = new string[0];
}
