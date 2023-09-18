using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class RoutingRuleElement
{
    public string Field { get; set; }
    public bool Required { get; set; }
    public Guid? RedirectTo { get; set; }

    public RoutingRuleElement()
    {
        
    }

    public static RoutingRuleElement ToMongoElement(RoutingRule routingRule)
    {
        return new RoutingRuleElement 
        {
            Field = routingRule.Field,
            Required = routingRule.Required,
            RedirectTo = !string.IsNullOrEmpty(routingRule.RedirectTo) ? Guid.Parse(routingRule.RedirectTo) : null
        };
    }

    public static RoutingRule ToDomainElement(string agentId, string agentName, RoutingRuleElement rule)
    {
        return new RoutingRule
        {
            AgentId = agentId,
            AgentName = agentName,
            Field = rule.Field,
            Required = rule.Required,
            RedirectTo = rule.RedirectTo?.ToString()
        };
    }
}
