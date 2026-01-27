using BotSharp.Abstraction.Agents.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public string Criteria { get; set; } = default!;
    public AgentRuleActionMongModel? Action { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Criteria = rule.Criteria,
            Action = AgentRuleActionMongModel.ToMongoModel(rule.Action)
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Criteria = rule.Criteria,
            Action = AgentRuleActionMongModel.ToDomainModel(rule.Action)
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleActionMongModel
{
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public BsonDocument? Config { get; set; }

    public static AgentRuleActionMongModel? ToMongoModel(AgentRuleAction? action)
    {
        if (action == null)
        {
            return null;
        }

        return new AgentRuleActionMongModel
        {
            Name = action.Name,
            Disabled = action.Disabled,
            Config = action.Config != null ? BsonDocument.Parse(action.Config.RootElement.GetRawText()) : null
        };
    }

    public static AgentRuleAction? ToDomainModel(AgentRuleActionMongModel? action)
    {
        if (action == null)
        {
            return null;
        }

        return new AgentRuleAction
        {
            Name = action.Name,
            Disabled = action.Disabled,
            Config = action.Config != null ? JsonDocument.Parse(action.Config.ToJson()) : null
        };
    }
}