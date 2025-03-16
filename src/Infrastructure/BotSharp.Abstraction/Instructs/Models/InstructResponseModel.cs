namespace BotSharp.Abstraction.Instructs.Models;

public class InstructResponseModel
{
    public string? AgentId { get; set; }
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string? TemplateName { get; set; }
    public string UserMessage { get; set; } = default!;
    public string? SystemInstruction { get; set; }
    public string CompletionText { get; set; } = default!;
}
