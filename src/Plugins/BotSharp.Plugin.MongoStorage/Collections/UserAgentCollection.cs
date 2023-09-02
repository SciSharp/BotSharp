namespace BotSharp.Plugin.MongoStorage.Collections;

public class UserAgentCollection : MongoBase
{
    public Guid UserId { get; set; }
    public Guid AgentId { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
