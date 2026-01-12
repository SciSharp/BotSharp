namespace BotSharp.Plugin.MessageQueue.Models;

/// <summary>
/// Payload for scheduled/delayed messages
/// </summary>
public class ScheduledMessagePayload
{
    public string Name { get; set; }
}

/// <summary>
/// Types of scheduled messages
/// </summary>
public enum ScheduledMessageType
{
    /// <summary>
    /// A reminder message to send to a conversation
    /// </summary>
    Reminder,

    /// <summary>
    /// A follow-up message for a previous conversation
    /// </summary>
    FollowUp,

    /// <summary>
    /// A scheduled task to execute
    /// </summary>
    Task
}

