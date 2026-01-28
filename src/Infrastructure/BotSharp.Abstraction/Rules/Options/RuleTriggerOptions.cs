using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Filter agents
    /// </summary>
    public AgentFilter? AgentFilter { get; set; }
}
