using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

/// <summary>
/// Decides whether a rule should be executed for the current request.
/// Implementations are resolved by <see cref="Type"/> in the rule engine,
/// so new criteria mechanisms can be added without changing the engine.
/// </summary>
public interface IRuleCriteriaEvaluator
{
    /// <summary>
    /// The criteria type this evaluator handles
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Evaluate the criteria for a single agent's rule.
    /// </summary>
    /// <param name="agent">The agent whose rule is being considered</param>
    /// <param name="trigger">The rule trigger</param>
    /// <param name="context">The per-request criteria context</param>
    /// <returns>True if the rule should be executed for this request.</returns>
    Task<bool> EvaluateAsync(Agent agent, IRuleTrigger trigger, RuleCriteriaContext context);
}
