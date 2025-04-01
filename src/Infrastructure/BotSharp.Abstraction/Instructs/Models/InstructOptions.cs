namespace BotSharp.Abstraction.Instructs.Models;

public class InstructOptions
{
    /// <summary>
    /// Llm provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Llm model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Agent
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Agent template name
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Conversation id. When this field is not null, it will get dialogs from conversation.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Data to fill in prompt
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
