using BotSharp.Abstraction.Conversations.Models;
namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = "openapi";
    public string? Template { get; set; }
}
