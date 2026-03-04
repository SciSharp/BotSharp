using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

/// <summary>
/// Base interface for rule conditions that can be evaluated by the RuleEngine
/// </summary>
public interface IRuleCondition : IRuleFlowUnit
{
    /// <summary>
    /// Evaluate the rule condition
    /// </summary>
    /// <param name="agent">The agent that triggered the rule</param>
    /// <param name="trigger">The rule trigger</param>
    /// <param name="context">The flow context</param>
    /// <returns>The condition evaluation result</returns>
    Task<RuleNodeResult> EvaluateAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleFlowContext context);
}

