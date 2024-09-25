namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructMessageModel : IncomingMessageModel
{
    /// <summary>
    /// System prompt
    /// </summary>
    public string? Instruction { get; set; }
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;
    public string? Template { get; set; }
}
