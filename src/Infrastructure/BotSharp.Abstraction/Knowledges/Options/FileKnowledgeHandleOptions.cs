namespace BotSharp.Abstraction.Knowledges.Options;

public class FileKnowledgeHandleOptions : LlmConfigBase
{
    /// <summary>
    /// Agent id
    /// </summary>
    public string? AgentId { get; set; }

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
