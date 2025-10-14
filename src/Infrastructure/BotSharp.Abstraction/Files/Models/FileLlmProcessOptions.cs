namespace BotSharp.Abstraction.Files.Models;

public class FileLlmProcessOptions
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
    /// Template name in Agent
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Data that is used to render instruction
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}
