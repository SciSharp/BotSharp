using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.MessageQueue.Models;

/// <summary>
/// Payload for delayed conversation messages
/// </summary>
public class ConversationMessagePayload
{
    /// <summary>
    /// The action to perform
    /// </summary>
    public ConversationAction Action { get; set; }

    /// <summary>
    /// The conversation ID
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// The agent ID to handle the message
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// The user ID associated with this message
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The role of the message sender (User, Assistant, Function, etc.)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The message content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Optional instruction for triggering an agent
    /// </summary>
    public string? Instruction { get; set; }

    /// <summary>
    /// Conversation states to set
    /// </summary>
    public List<MessageState>? States { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Actions that can be performed on a conversation
/// </summary>
public enum ConversationAction
{
    /// <summary>
    /// Send a message to the conversation
    /// </summary>
    SendMessage,

    /// <summary>
    /// Trigger an agent to respond
    /// </summary>
    TriggerAgent,

    /// <summary>
    /// Send a notification to the conversation
    /// </summary>
    Notify
}

