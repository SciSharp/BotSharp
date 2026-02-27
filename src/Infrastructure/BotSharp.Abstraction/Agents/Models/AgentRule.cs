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

public class AgentRuleConfigBase
{
    public virtual string Name { get; set; }

    public virtual bool Disabled { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual JsonDocument? Config { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual string? JsonConfig { get; set; }
}