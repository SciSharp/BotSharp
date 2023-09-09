using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Repositories.Records;

public class RoutingProfileRecord : RecordBase
{
    public string Name { get; set; }
    public List<string> AgentIds { get; set; }
}
