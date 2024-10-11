namespace BotSharp.Abstraction.Processors.Models;

public class LlmBaseRequest
{
    public string Provider { get; set; }
    public string Model { get; set; }
    public string? AgentId { get; set; }
    public string? TemplateName { get; set; }
}
