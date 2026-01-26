using System.Text.Json;

namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("criteria")]
    public string Criteria { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Adaptive configuration for rule actions.
    /// This flexible JSON document can store any action-specific configuration.
    /// The structure depends on the action type:
    /// - For "Http" action: contains http_context with base_url, relative_url, method, etc.
    /// - For "MessageQueue" action: contains mq_config with topic_name, routing_key, etc.
    /// - For custom actions: can contain any custom configuration structure
    /// </summary>
    [JsonPropertyName("action_config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonDocument? ActionConfig { get; set; }
}
