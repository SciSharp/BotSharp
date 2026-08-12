using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public string? Message { get; set; }
    public RuleCriteriaMongoModel? Criteria { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Message = rule.Message,
            Criteria = RuleCriteriaMongoModel.ToMongoModel(rule.Criteria)
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Message = rule.Message,
            Criteria = RuleCriteriaMongoModel.ToDomainModel(rule.Criteria)
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class RuleCriteriaMongoModel
{
    public string? Mode { get; set; }
    public string? Criteria { get; set; }

    public static RuleCriteriaMongoModel? ToMongoModel(RuleCriteria? criteria)
    {
        if (criteria == null)
        {
            return null;
        }

        return new RuleCriteriaMongoModel
        {
            Mode = criteria.Mode,
            Criteria = criteria.Criteria
        };
    }

    public static RuleCriteria? ToDomainModel(RuleCriteriaMongoModel? criteria)
    {
        if (criteria == null)
        {
            return null;
        }

        return new RuleCriteria
        {
            Mode = criteria.Mode,
            Criteria = criteria.Criteria
        };
    }
}
