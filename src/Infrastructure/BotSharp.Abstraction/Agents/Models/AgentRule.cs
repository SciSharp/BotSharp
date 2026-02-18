using System.Text.Json;

namespace BotSharp.Abstraction.Agents.Models;

public class AgentRule
{
    [JsonPropertyName("trigger_name")]
    public string TriggerName { get; set; } = string.Empty;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("rule_criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentRuleCriteria? RuleCriteria { get; set; }

    [JsonPropertyName("rule_actions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<AgentRuleAction> RuleActions { get; set; } = [];
}

public class AgentRuleCriteria : AgentRuleConfigBase
{
    /// <summary>
    /// Criteria
    /// </summary>
    [JsonPropertyName("criteria_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CriteriaText { get; set; } = string.Empty;

    /// <summary>
    /// Adaptive configuration for rule criteria.
    /// This flexible JSON document can store any criteria-specific configuration.
    /// The structure depends on the criteria executor
    /// </summary>
    [JsonPropertyName("config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public override JsonDocument? Config { get; set; }
}

public class AgentRuleAction : AgentRuleConfigBase
{
    /// <summary>
    /// Adaptive configuration for rule actions.
    /// This flexible JSON document can store any action-specific configuration.
    /// The structure depends on the action type:
    /// - For "Http" action: contains http_context with base_url, relative_url, method, etc.
    /// - For "MessageQueue" action: contains mq_config with topic_name, routing_key, etc.
    /// - For custom actions: can contain any custom configuration structure
    /// </summary>
    [JsonPropertyName("config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public override JsonDocument? Config { get; set; }
}

public class AgentRuleConfigBase
{
    [JsonPropertyName("name")]
    public virtual string Name { get; set; }

    [JsonPropertyName("disabled")]
    public virtual bool Disabled { get; set; }

    [JsonPropertyName("config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual JsonDocument? Config { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? JsonConfig { get; set; }
}