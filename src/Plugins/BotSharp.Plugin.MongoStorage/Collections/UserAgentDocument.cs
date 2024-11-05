namespace BotSharp.Plugin.MongoStorage.Collections;

public class UserAgentDocument : MongoBase
{
    public string UserId { get; set; }
    public string AgentId { get; set; }
    public IEnumerable<string> Actions { get; set; } = [];
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
