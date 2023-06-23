using BotSharp.Abstraction.Infrastructures.ContentTransfers;

namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// IChatServiceZone is used to manage the chat function. 
/// When user send message to controller, all the registered service zone will process the message respectively.
/// </summary>
public interface IChatServiceZone : IServiceZone
{
}
