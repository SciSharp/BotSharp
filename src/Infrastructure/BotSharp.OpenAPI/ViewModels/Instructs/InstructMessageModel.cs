using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Conversations.Models;
namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;
    public string? Template { get; set; }
}
