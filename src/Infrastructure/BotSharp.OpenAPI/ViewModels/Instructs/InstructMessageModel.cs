using BotSharp.OpenAPI.ViewModels.Conversations;

namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructMessageModel : NewMessageModel
{
    public string? TemplateName { get; set; }
}
