using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentRuleMongoElement
{
    public string TriggerName { get; set; }
    public bool Disabled { get; set; }
    public string EventName { get; set; }
    public string EntityType { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            EventName = rule.EventName,
            EntityType = rule.EntityType
        };
    }
}
