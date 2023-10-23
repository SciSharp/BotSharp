namespace BotSharp.Plugin.MongoStorage.Collections;

public class ExectionLogCollection : MongoBase
{
    public string ConversationId { get; set; }
    public List<string> Logs { get; set; }
}
