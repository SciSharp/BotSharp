namespace BotSharp.Abstraction.Agents.Models;

public class AgentEventRule
{
    public string Name { get; set; }
    public bool Disabled { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; }

    [JsonPropertyName("event_type")]
    public string EntityType { get; set; }
}
