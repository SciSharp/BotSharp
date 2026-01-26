using BotSharp.Abstraction.Agents.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentRuleMongoElement
{
    public string TriggerName { get; set; } = default!;
    public bool Disabled { get; set; }
    public string Criteria { get; set; } = default!;
    public string? Action { get; set; }
    public BsonDocument? ActionConfig { get; set; }

    public static AgentRuleMongoElement ToMongoElement(AgentRule rule)
    {
        return new AgentRuleMongoElement
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Criteria = rule.Criteria,
            Action = rule.Action,
            ActionConfig = rule.ActionConfig != null ? BsonDocument.Parse(rule.ActionConfig.RootElement.GetRawText()) : null
        };
    }

    public static AgentRule ToDomainElement(AgentRuleMongoElement rule)
    {
        return new AgentRule
        {
            TriggerName = rule.TriggerName,
            Disabled = rule.Disabled,
            Criteria = rule.Criteria,
            Action = rule.Action,
            ActionConfig = rule.ActionConfig != null ? JsonDocument.Parse(rule.ActionConfig.ToJson()) : null
        };
    }
}
