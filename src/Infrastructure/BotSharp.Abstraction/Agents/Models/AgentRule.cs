namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    public string Name { get; set; }
    public bool Disabled { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; }

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; }
}
