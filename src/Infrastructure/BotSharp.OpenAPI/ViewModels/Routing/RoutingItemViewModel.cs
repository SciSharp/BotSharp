using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Routing;

public class RoutingItemViewModel
{
    public string AgentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> RequiredFields { get; set; } = new List<string>();
    public string? RedirectTo { get; set; }
    public bool Disabled { get; set; }

    public static RoutingItemViewModel FromRoutingItem(RoutingItem routingItem)
    {
        return new RoutingItemViewModel
        {
            AgentId = routingItem.AgentId,
            Name = routingItem.Name,
            Description = routingItem.Description,
            RequiredFields = routingItem.RequiredFields,
            RedirectTo = routingItem.RedirectTo,
            Disabled = routingItem.Disabled
        };
    }
}
