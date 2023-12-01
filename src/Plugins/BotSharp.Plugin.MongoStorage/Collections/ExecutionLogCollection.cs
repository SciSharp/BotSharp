namespace BotSharp.Plugin.MongoStorage.Collections;

public class ExecutionLogCollection : MongoBase
{
    public string ConversationId { get; set; }
    public List<string> Logs { get; set; }
}
