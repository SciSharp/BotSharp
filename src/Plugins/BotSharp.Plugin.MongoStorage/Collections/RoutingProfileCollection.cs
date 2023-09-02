namespace BotSharp.Plugin.MongoStorage.Collections;

public class RoutingProfileCollection : MongoBase
{
    public string Name { get; set; }
    public List<Guid> AgentIds { get; set; }
}
