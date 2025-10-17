namespace BotSharp.Abstraction.Models;

public class LlmConfigBase : LlmProviderModel
{
    /// <summary>
    /// Llm maximum output tokens
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Llm reasoning effort level
    /// </summary>
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
}