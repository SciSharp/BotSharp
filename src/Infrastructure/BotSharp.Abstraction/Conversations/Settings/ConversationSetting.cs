namespace BotSharp.Abstraction.Conversations.Settings;

public class ConversationSetting
{
    public string DataDir { get; set; }
    public string ChatCompletion { get; set; }
    public bool EnableKnowledgeBase { get; set; }
    public bool ShowVerboseLog { get; set; }
    public int MaxRecursiveDepth { get; set; } = 3;
    public bool EnableLlmCompletionLog { get; set; }
    public bool EnableExecutionLog { get; set; }
    public bool EnableContentLog { get; set; }
    public bool EnableStateLog { get; set; }
    public bool EnableTranslationMemory { get; set; }
    public CleanConversationSetting CleanSetting { get; set; } = new CleanConversationSetting();
    public RateLimitSetting RateLimit { get; set; } = new RateLimitSetting();
}

public class CleanConversationSetting
{
    public bool Enable { get; set; }
    public int BatchSize { get; set; }
    public int MessageLimit { get; set; }
    public int BufferHours { get; set; }

}
