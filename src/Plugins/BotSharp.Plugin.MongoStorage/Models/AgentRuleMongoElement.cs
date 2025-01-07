using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentRuleMongoElement
{
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public string EventName { get; set; }
    public string EntityType { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            Name = rule.Name,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            Name = rule.Name,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }
}
