namespace BotSharp.Abstraction.Models;

public class LlmConfigBase : LlmProviderModel
{
    /// <summary>
    /// Llm maximum output tokens
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Llm reasoning effort level, thinking level
    /// </summary>
    [JsonPropertyName("reasoning_effort_level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffortLevel { get; set; }
}

public class LlmProviderModel
{
    [JsonPropertyName("provider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Provider { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(Provider) && !string.IsNullOrEmpty(Model);
}