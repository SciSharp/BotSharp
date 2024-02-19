namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationStateLogDocument : MongoBase
{
    public string ConversationId { get; set; }
    public string MessageId { get; set; }
    public Dictionary<string, string> States { get; set; }
    public DateTime CreateTime { get; set; }
}
