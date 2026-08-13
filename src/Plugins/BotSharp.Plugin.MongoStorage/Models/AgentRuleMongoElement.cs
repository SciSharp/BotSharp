using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public string? Message { get; set; }
    public RuleCriteriaConfigMongoModel? CriteriaConfig { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Message = rule.Message,
            CriteriaConfig = RuleCriteriaConfigMongoModel.ToMongoModel(rule.CriteriaConfig)
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Message = rule.Message,
            CriteriaConfig = RuleCriteriaConfigMongoModel.ToDomainModel(rule.CriteriaConfig)
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class RuleCriteriaConfigMongoModel
{
    public string? Mode { get; set; }
    public string? Criteria { get; set; }

    public static RuleCriteriaConfigMongoModel? ToMongoModel(RuleCriteriaConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new RuleCriteriaConfigMongoModel
        {
            Mode = config.Mode,
            Criteria = config.Criteria
        };
    }

    public static RuleCriteriaConfig? ToDomainModel(RuleCriteriaConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new RuleCriteriaConfig
        {
            Mode = config.Mode,
            Criteria = config.Criteria
        };
    }
}
