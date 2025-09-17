namespace BotSharp.Abstraction.Models;

public class LlmConfigBase : LlmBase
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

public class LlmBase
{
    /// <summary>
    /// Llm provider
    /// </summary>
    public string? LlmProvider { get; set; }

    /// <summary>
    /// Llm model
    /// </summary>
    public string? LlmModel { get; set; }
}