using BotSharp.Abstraction.Repositories.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationStatesCollection : MongoBase
{
    public Guid ConversationId { get; set; }
    public List<KeyValueModel> State { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
