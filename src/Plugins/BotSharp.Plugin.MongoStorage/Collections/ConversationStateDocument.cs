namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationStateDocument : MongoBase
{
    public string ConversationId { get; set; }
    public List<StateMongoElement> States { get; set; } = new List<StateMongoElement>();
    public List<BreakpointMongoElement> Breakpoints { get; set; } = new List<BreakpointMongoElement>();
}
