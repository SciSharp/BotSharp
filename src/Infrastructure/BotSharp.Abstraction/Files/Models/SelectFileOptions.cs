namespace BotSharp.Abstraction.Files.Models;

public class SelectFileOptions
{
    public string? Provider { get; set; }
    public string? ModelId { get; set; }
    public string? AgentId { get; set; }
    public string? Template { get; set; }
    public string? Description { get; set; }
    public bool IncludeBotFile { get; set; }
    public bool FromBreakpoint { get; set; }
    public int? Offset { get; set; }
    public IEnumerable<string>? ContentTypes { get; set; }
}
