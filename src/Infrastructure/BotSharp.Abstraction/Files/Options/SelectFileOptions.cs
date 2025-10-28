namespace BotSharp.Abstraction.Files.Options;

public class SelectFileOptions : LlmConfigBase
{
    /// <summary>
    /// Agent id
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Template (prompt) name
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// User description to select files
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether include bot generated files
    /// </summary>
    public bool IsIncludeBotFiles { get; set; }

    /// <summary>
    /// Conversation breakpoint
    /// </summary>
    public bool FromBreakpoint { get; set; }

    /// <summary>
    /// The maximum number of messages
    /// </summary>
    public int? MessageLimit { get; set; }

    /// <summary>
    /// Whehter attach files to messages
    /// </summary>
    public bool IsAttachFiles { get; set; }

    /// <summary>
    /// File content types. If null, all types of files will be retrived
    /// </summary>
    public IEnumerable<string>? ContentTypes { get; set; }

    /// <summary>
    /// Data that can be used to fill in the prompt
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}
