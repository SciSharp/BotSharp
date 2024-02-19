namespace BotSharp.Abstraction.Loggers.Models;

public class LlmCompletionLog
{
    public string ConversationId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime CreateDateTime { get; set; } = DateTime.UtcNow;
}
