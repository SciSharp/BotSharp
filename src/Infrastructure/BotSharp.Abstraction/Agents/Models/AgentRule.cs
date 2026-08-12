namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    /// <summary>
    /// Message sent to agent
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RuleCriteria? Criteria { get; set; }
}

public class RuleCriteria
{
    /// <summary>
    /// Criteria mode: llm, python script, etc.
    /// Takes precedence over the mode carried on the trigger options.
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Criteria text
    /// </summary>
    [JsonPropertyName("criteria")]
    public string? Criteria { get; set; }
}
