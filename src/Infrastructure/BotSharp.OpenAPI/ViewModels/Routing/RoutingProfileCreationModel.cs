using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Routing;

public class RoutingProfileCreationModel
{
    public string Name { get; set; }
    public string[] AgentIds { get; set; }

    public RoutingProfile ToRoutingProfile()
    {
        return new RoutingProfile
        {
            Name = Name,
            AgentIds = AgentIds,
        };
    }
}
