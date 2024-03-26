using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationStateDocument : MongoBase
{
    public string ConversationId { get; set; }
    public List<StateMongoElement> States { get; set; }
    public List<BreakpointMongoElement> Breakpoints { get; set; }
}
