using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class RoutingRuleMongoElement
{
    public string Field { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public string? RedirectTo { get; set; }

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
            RedirectTo = routingRule.RedirectTo
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
            RedirectTo = rule.RedirectTo
        };
    }
}
