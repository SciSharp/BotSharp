namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;
}
