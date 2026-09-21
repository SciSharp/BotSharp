namespace BotSharp.Abstraction.Conversations.Settings;

public class ConversationSetting
{
    public string DataDir { get; set; } = "conversations";
    public string ChatCompletion { get; set; }
    public bool EnableKnowledgeBase { get; set; }
    public bool ShowVerboseLog { get; set; }
    public int MaxRecursiveDepth { get; set; } = 3;
    public bool EnableLlmCompletionLog { get; set; }
    public bool EnableExecutionLog { get; set; }
    public bool EnableContentLog { get; set; }
    public bool EnableStateLog { get; set; }
    public bool EnableTranslationMemory { get; set; }
    public CleanConversationSetting CleanSetting { get; set; } = new();
    public ToolResultTrimSetting ToolResultTrim { get; set; } = new();
    public RateLimitSetting RateLimit { get; set; } = new();
    public FileSelectSetting? FileSelect { get; set; }
}

/// <summary>
/// Caps what an older turn's tool result costs in the prompt. The turn that ran the tool always
/// sees it whole; only what history replays is shortened.
/// </summary>
public class ToolResultTrimSetting
{
    public bool Enable { get; set; } = true;

    /// <summary>How many of the most recent turns keep their tool results verbatim.</summary>
    public int KeepTurns { get; set; } = 2;

    /// <summary>A result longer than this, and older than <see cref="KeepTurns"/>, is shortened.</summary>
    public int MaxLength { get; set; } = 500;
}

public class CleanConversationSetting
{
    public bool Enable { get; set; }
    public int BatchSize { get; set; }
    public int MessageLimit { get; set; }
    public int BufferHours { get; set; }
    public int LogRetentionDays { get; set; }
    public int LogBatchSize { get; set; } = 2000;
    public IEnumerable<string> ExcludeAgentIds { get; set; } = new List<string>();
}

public class FileSelectSetting : LlmConfigBase
{
    public int? MessageLimit { get; set; }
}
