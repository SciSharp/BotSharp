namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogCollection : MongoBase
{
    public string ConversationId { get; set; }
    public string Dialog { get; set; }
}
