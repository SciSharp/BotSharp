using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

/// <summary>
/// Base interface for rule actions that can be executed by the RuleEngine
/// </summary>
public interface IRuleAction : IRuleFlowUnit
{
    /// <summary>
    /// Execute the rule action
    /// </summary>
    /// <param name="agent">The agent that triggered the rule</param>
    /// <param name="trigger">The rule trigger</param>
    /// <param name="context">The flow context</param>
    /// <returns>The action execution result</returns>
    Task<RuleNodeResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context);
}