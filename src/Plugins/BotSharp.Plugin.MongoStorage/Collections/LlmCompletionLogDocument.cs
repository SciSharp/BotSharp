namespace BotSharp.Plugin.MongoStorage.Collections;

public class LlmCompletionLogDocument : MongoBase
{
    public string ConversationId { get; set; }
    public string MessageId { get; set; }
    public string AgentId { get; set; }
    public string Prompt { get; set; }
    public string? Response { get; set; }
    public DateTime CreateDateTime { get; set; }
}
