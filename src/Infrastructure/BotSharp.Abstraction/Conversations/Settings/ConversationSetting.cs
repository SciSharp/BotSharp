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
    public RateLimitSetting RateLimit { get; set; } = new();
    public FileSelectSetting? FileSelect { get; set; }
    public AutoCompressionSetting AutoCompression { get; set; } = new();
}

/// <summary>
/// Automatically compress a long conversation once its uncompressed message count grows large,
/// keeping recent turns verbatim and replacing older turns with a generated summary.
/// </summary>
public class AutoCompressionSetting
{
    /// <summary>
    /// Enable the context layer: summarize old turns and set a breakpoint so the LLM sees
    /// [summary] + recent turns. Fully reversible, no stored data is removed.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enable the storage layer: physically archive old raw dialogs out of the hot dialog record
    /// and replace them with a single summary dialog. Requires <see cref="Enabled"/>.
    /// </summary>
    public bool CompactStorage { get; set; } = false;

    /// <summary>
    /// Compress when (DialogCount - CompactedDialogCount) reaches this many messages.
    /// </summary>
    public int TriggerMessageCount { get; set; } = 500;

    /// <summary>
    /// Number of most recent messages always kept verbatim (never summarized).
    /// </summary>
    public int KeepRecentCount { get; set; } = 100;

    /// <summary>
    /// Agent that owns the summary template and whose LLM config (provider, model, max output tokens,
    /// reasoning level, response format) is used to generate the summary.
    /// When not set, the conversation's own agent is used.
    /// </summary>
    public string SummaryAgentId { get; set; } = string.Empty;

    /// <summary>
    /// Template used to generate the compression summary.
    /// </summary>
    public string SummaryTemplateName { get; set; } = "conversation.compression";
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
