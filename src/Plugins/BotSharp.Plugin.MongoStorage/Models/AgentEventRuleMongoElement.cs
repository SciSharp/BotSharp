using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentEventRuleMongoElement
{
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public string EventName { get; set; }
    public string EntityType { get; set; }

    public static AgentEventRuleMongoElement ToMongoElement(AgentEventRule rule)
    {
        return new AgentEventRuleMongoElement
        {
            Name = rule.Name,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }

    public static AgentEventRule ToDomainElement(AgentEventRuleMongoElement rule)
    {
        return new AgentEventRule
        {
            Name = rule.Name,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }
}
