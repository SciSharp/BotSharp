namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationStateLogDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string MessageId { get; set; } = default!;
    public Dictionary<string, string> States { get; set; } = [];
    public DateTime CreatedTime { get; set; }
}
