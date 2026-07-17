using System.Text.Json.Serialization;

namespace BotSharp.Core.Rules.Criteria.Llm;

/// <summary>
/// Settings for <see cref="LlmCriteriaEvaluator"/>, parsed from
/// <c>CriteriaOptions.Data</c>.
/// </summary>
public class LlmCriteriaSettings
{
    /// <summary>
    /// The agent that hosts the criteria-check template.
    /// Defaults to the built-in Rules agent.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    /// <summary>
    /// The template used as the system prompt. Defaults to "criteria_check".
    /// </summary>
    [JsonPropertyName("template_name")]
    public string? TemplateName { get; set; }

    /// <summary>
    /// Json arguments as an input value
    /// </summary>
    [JsonPropertyName("argument_content")]
    public JsonDocument? ArgumentContent { get; set; }
}
