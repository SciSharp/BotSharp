using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class RoutingRuleUpdateModel
{
    public string Field { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public string? RedirectTo { get; set; }

    public RoutingRuleUpdateModel()
    {
        
    }

    public static RoutingRule ToDomainElement(RoutingRuleUpdateModel model)
    {
        return new RoutingRule 
        { 
            Field = model.Field,
            Description = model.Description,
            Required = model.Required,
            RedirectTo = model.RedirectTo
        };
    }
}
