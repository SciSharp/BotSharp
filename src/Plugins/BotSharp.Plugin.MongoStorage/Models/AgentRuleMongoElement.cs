using BotSharp.Abstraction.Agents.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public AgentRuleCriteriaMongoModel? RuleCriteria { get; set; }
    public List<AgentRuleActionMongoElement> RuleActions { get; set; } = [];

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            RuleCriteria = AgentRuleCriteriaMongoModel.ToMongoModel(rule.RuleCriteria),
            RuleActions = rule.RuleActions?.Where(x => x != null).Select(x => AgentRuleActionMongoElement.ToMongoElement(x)!)?.ToList() ?? []
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            RuleCriteria = AgentRuleCriteriaMongoModel.ToDomainModel(rule.RuleCriteria),
            RuleActions = rule.RuleActions?.Where(x => x != null).Select(x => AgentRuleActionMongoElement.ToDomainElement(x)!)?.ToList() ?? []
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleCriteriaMongoModel : AgentRuleConfigMongoModel
{
    public string CriteriaText { get; set; }

    public static AgentRuleCriteriaMongoModel? ToMongoModel(AgentRuleCriteria? criteria)
    {
        if (criteria == null)
        {
            return null;
        }

        return new AgentRuleCriteriaMongoModel
        {
            Name = criteria.Name,
            CriteriaText = criteria.CriteriaText,
            Disabled = criteria.Disabled,
            Config = criteria.Config != null ? BsonDocument.Parse(criteria.Config.RootElement.GetRawText()) : null
        };
    }

    public static AgentRuleCriteria? ToDomainModel(AgentRuleCriteriaMongoModel? criteria)
    {
        if (criteria == null)
        {
            return null;
        }

        return new AgentRuleCriteria
        {
            Name = criteria.Name,
            CriteriaText = criteria.CriteriaText,
            Disabled = criteria.Disabled,
            Config = criteria.Config != null ? JsonDocument.Parse(criteria.Config.ToJson()) : null
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleActionMongoElement : AgentRuleConfigMongoModel
{
    public string? SkippingExpression { get; set; }

    public static AgentRuleActionMongoElement? ToMongoElement(AgentRuleAction? action)
    {
        if (action == null)
        {
            return null;
        }

        return new AgentRuleActionMongoElement
        {
            Name = action.Name,
            Disabled = action.Disabled,
            Config = action.Config != null ? BsonDocument.Parse(action.Config.RootElement.GetRawText()) : null,
            SkippingExpression = action.SkippingExpression
        };
    }

    public static AgentRuleAction? ToDomainElement(AgentRuleActionMongoElement? action)
    {
        if (action == null)
        {
            return null;
        }

        return new AgentRuleAction
        {
            Name = action.Name,
            Disabled = action.Disabled,
            Config = action.Config != null ? JsonDocument.Parse(action.Config.ToJson()) : null,
            SkippingExpression = action.SkippingExpression
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleConfigMongoModel
{
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public BsonDocument? Config { get; set; }
}