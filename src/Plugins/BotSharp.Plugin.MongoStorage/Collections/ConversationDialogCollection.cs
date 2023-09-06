namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogCollection : MongoBase
{
    public Guid ConversationId { get; set; }
    public string Dialog { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
