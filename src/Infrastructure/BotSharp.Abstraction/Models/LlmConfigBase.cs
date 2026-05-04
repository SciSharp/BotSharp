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
    public string? Provider { get; set; }
    public string? Model { get; set; }
}