namespace BotSharp.Abstraction.Models;

/// <summary>
/// Define a message ID to extend message-level applications, such as model fees, token usage, and data collection
/// </summary>
public interface ITrackableMessage
{
    string MessageId { get; set; }
}
