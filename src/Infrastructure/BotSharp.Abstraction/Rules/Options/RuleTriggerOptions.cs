using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Filter agents
    /// </summary>
    public AgentFilter? AgentFilter { get; set; }

    /// <summary>
    /// Criteria options for validating whether the rule should be triggered
    /// </summary>
    public RuleCriteriaOptions? Criteria { get; set; }
}
