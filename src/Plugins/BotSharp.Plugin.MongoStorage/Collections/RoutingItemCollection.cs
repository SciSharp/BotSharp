namespace BotSharp.Plugin.MongoStorage.Collections;

public class RoutingItemCollection : MongoBase
{
    public Guid AgentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> RequiredFields { get; set; }
    public Guid RedirectTo { get; set; }
    public bool Disabled { get; set; }
}
