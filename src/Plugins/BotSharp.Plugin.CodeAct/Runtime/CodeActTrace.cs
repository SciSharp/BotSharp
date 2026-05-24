namespace BotSharp.Plugin.CodeAct.Runtime;

public class CodeActTrace
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Event { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object?> Attributes { get; set; } = [];
}
