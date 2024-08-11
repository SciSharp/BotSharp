namespace BotSharp.Abstraction.Files.Models;

public class SelectFileOptions
{
    /// <summary>
    /// Llm provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Llm model id
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Agent id
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Template (prompt) name
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Description that user provides to select files
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether include bot generated files
    /// </summary>
    public bool IncludeBotFile { get; set; }

    /// <summary>
    /// Conversation breakpoint
    /// </summary>
    public bool FromBreakpoint { get; set; }

    /// <summary>
    /// Message offset from last
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// File content types. If null, all types of files will be retrived
    /// </summary>
    public IEnumerable<string>? ContentTypes { get; set; }
}
