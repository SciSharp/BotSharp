namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; }

    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; }

    [JsonPropertyName("criteria")]
    public string Criteria { get; set; }
}
