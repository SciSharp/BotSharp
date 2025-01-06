using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Rules.Hooks;

public interface IEventRuleHook
{
    void AddRules(List<AgentEventRule> rules);
}
