using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class RoutingRuleMongoElement
{
    public string Field { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public Guid? RedirectTo { get; set; }

    public RoutingRuleMongoElement()
    {
        
    }

    public static RoutingRuleMongoElement ToMongoElement(RoutingRule routingRule)
    {
        return new RoutingRuleMongoElement 
        {
            Field = routingRule.Field,
            Description = routingRule.Description,
            Required = routingRule.Required,
            RedirectTo = !string.IsNullOrEmpty(routingRule.RedirectTo) ? Guid.Parse(routingRule.RedirectTo) : null
        };
    }

    public static RoutingRule ToDomainElement(string agentId, string agentName, RoutingRuleMongoElement rule)
    {
        return new RoutingRule
        {
            AgentId = agentId,
            AgentName = agentName,
            Field = rule.Field,
            Description = rule.Description,
            Required = rule.Required,
            RedirectTo = rule.RedirectTo?.ToString()
        };
    }
}
