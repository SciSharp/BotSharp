namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogDocument : MongoBase
{
    public string ConversationId { get; set; }
    public string AgentId { get; set; }
    public DateTime UpdatedTime { get; set; }
    public List<DialogMongoElement> Dialogs { get; set; }
}
