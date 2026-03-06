namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleConfig? Config { get; set; }
}

public class RuleConfig
{
    [JsonPropertyName("topology_provider")]
    public string? TopologyProvider { get; set; }
}