namespace BotSharp.Plugin.CodeAct.Runtime;

public class CodeActRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string? ConversationId { get; set; }
    public string? MessageId { get; set; }
    public string? AgentId { get; set; }
    public string? UserId { get; set; }
    public string Language { get; set; } = "text";
    public string Code { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public bool ReadOnly { get; set; } = true;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
