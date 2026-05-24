namespace BotSharp.Plugin.CodeAct.Runtime;

public class CodeActResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    public object? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public List<CodeActTrace> Trace { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
