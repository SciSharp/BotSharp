using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = "openapi";
}
