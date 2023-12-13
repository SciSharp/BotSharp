namespace BotSharp.Plugin.MongoStorage.Collections;

public class ExecutionLogDocument : MongoBase
{
    public string ConversationId { get; set; }
    public List<string> Logs { get; set; }
}
