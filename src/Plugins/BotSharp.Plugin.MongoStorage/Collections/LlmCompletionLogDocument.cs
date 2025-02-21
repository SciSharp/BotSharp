namespace BotSharp.Plugin.MongoStorage.Collections;

public class LlmCompletionLogDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public string MessageId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string Prompt { get; set; } = default!;
    public string? Response { get; set; }
    public DateTime CreatedTime { get; set; }
}
