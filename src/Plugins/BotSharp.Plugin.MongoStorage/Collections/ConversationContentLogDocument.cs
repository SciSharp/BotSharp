namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationContentLogDocument : MongoBase
{
    public string ConversationId { get; set; }
    public string MessageId { get; set; }
    public string? Name { get; set; }
    public string Role { get; set; }
    public string Source { get; set; }
    public string Content { get; set; }
    public DateTime CreateTime { get; set; }
}
