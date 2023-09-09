using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Routing;

public class RoutingProfileViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> AgentIds { get; set; }

    public static RoutingProfileViewModel FromRoutingProfile(RoutingProfile profile)
    {
        return new RoutingProfileViewModel
        {
            Id = profile.Id,
            Name = profile.Name,
            AgentIds = profile.AgentIds,
        };
    }
}
