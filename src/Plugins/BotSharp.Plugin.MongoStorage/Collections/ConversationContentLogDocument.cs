namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationContentLogDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public string MessageId { get; set; } = default!;
    public string? Name { get; set; }
    public string? AgentId { get; set; }
    public string Role { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime CreatedTime { get; set; }
}
