namespace BotSharp.Abstraction.Files.Options;

public class FileHandleOptions
{
    /// <summary>
    /// Llm provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// llm model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Llm maximum output tokens
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Reasoning effort level
    /// </summary>
    public string? ReasoningEfforLevel { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string? Instruction { get; set; }

    /// <summary>
    /// Message from user
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// Template name in Agent
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// The upstream where the file llm is invoked
    /// </summary>
    public string? InvokeFrom { get; set; }

    /// <summary>
    /// Data that is used to render instruction
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}
