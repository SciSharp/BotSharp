namespace BotSharp.Abstraction.Instructs.Models;

public class InstructOptions
{
    /// <summary>
    /// Llm provider
    /// </summary>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// Llm model
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// Conversation id. When this field is not null, it will get dialogs from conversation.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// The single message. It can be append to the whole dialogs or sent alone.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Data to fill in prompt
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
