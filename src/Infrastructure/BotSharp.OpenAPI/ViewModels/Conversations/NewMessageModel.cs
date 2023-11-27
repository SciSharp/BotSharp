using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;
}
