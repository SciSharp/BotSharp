namespace BotSharp.Abstraction.Browsing.Models;

public class MessageInfo
{
    public string AgentId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ContextId { get; set; } = null!;
    public string? MessageId { get; set; }
    public string? TaskId { get; set; }
    public int StepNum { get; set; }
}
