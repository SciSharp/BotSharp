using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingService
{
    Task<List<RoutingItem>> CreateRoutingItems(List<RoutingItem> routingItems);
    Task<List<RoutingProfile>> CreateRoutingProfiles(List<RoutingProfile> routingProfiles);
    Task DeleteRoutingItems();
    Task DeleteRoutingProfiles();
}
