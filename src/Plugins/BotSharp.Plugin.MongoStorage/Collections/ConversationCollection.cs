using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationCollection : MongoBase
{
    public string AgentId { get; set; }
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Status { get; set; }
    public List<StateKeyValue> States { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
