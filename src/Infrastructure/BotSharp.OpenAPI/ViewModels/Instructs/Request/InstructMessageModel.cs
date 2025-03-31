namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructMessageModel : IncomingMessageModel
{
    /// <summary>
    /// System prompt
    /// </summary>
    public string? Instruction { get; set; }
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;
    public string? Template { get; set; }
    public List<InstructFileModel> Files { get; set; } = [];
}


public class IncomingInstructRequest : IncomingMessageModel
{
    public string? AgentId { get; set; }
    public string? Instruction { get; set; }
    public string? Template { get; set; }
    public List<InstructFileModel> Files { get; set; } = [];
}