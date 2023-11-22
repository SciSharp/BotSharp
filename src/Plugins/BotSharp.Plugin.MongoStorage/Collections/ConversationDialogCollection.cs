using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogCollection : MongoBase
{
    public string ConversationId { get; set; }
    public List<DialogMongoElement> Dialogs { get; set; }
}
