using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Routing;

public class RoutingProfileViewModel
{
    public string Name { get; set; }
    public string[] AgentIds { get; set; }

    public static RoutingProfileViewModel FromRoutingProfile(RoutingProfile profile)
    {
        return new RoutingProfileViewModel
        {
            Name = profile.Name,
            AgentIds = profile.AgentIds,
        };
    }
}
