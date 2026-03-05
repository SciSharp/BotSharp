using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public RuleConfigMongoModel? Config { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Config = RuleConfigMongoModel.ToMongoModel(rule.Config)
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Config = RuleConfigMongoModel.ToDomainModel(rule.Config)
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class RuleConfigMongoModel
{
    public string? Topology { get; set; }
    public string? Provider { get; set; }

    public static RuleConfigMongoModel? ToMongoModel(RuleConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        return new RuleConfigMongoModel
        {
            Topology = config.Topology,
            Provider = config.Provider
        };
    }

    public static RuleConfig? ToDomainModel(RuleConfigMongoModel? config)
    {
        if (config == null)
        {
            return null;
        }

        return new RuleConfig
        {
            Topology = config.Topology,
            Provider = config.Provider
        };
    }
}
