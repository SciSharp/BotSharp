namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogCollection : MongoBase
{
    public Guid ConversationId { get; set; }
    public string Dialog { get; set; }
}
