using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Repositories.Records;

public class RoutingProfileRecord : RecordBase
{
    public string Name { get; set; }
    public List<string> AgentIds { get; set; }

    public RoutingProfile ToRoutingProfile()
    {
        return new RoutingProfile
        {
            Name = Name,
            AgentIds = AgentIds.ToArray(),
        };
    }

    public static RoutingProfileRecord FromRoutingProfile(RoutingProfile profile)
    {
        return new RoutingProfileRecord
        {
            Name = profile.Name,
            AgentIds = profile.AgentIds.ToList(),
        };
    }
}
