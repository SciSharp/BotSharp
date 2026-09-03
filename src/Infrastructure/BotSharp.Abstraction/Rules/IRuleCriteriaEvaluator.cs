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
    /// <param name="agentRule">
    /// The rule being considered. An agent can carry more than one rule on the same trigger, so the
    /// evaluator is handed the one under evaluation rather than resolving it from the agent itself.
    /// </param>
    /// <param name="trigger">The rule trigger</param>
    /// <param name="context">The per-request criteria context</param>
    /// <returns>
    /// True if the rule should be executed for this request, false if it should be skipped,
    /// or null when the evaluator could not produce an answer (missing script/template,
    /// failed execution, error).
    /// </returns>
    Task<bool?> EvaluateAsync(Agent agent, AgentRule agentRule, IRuleTrigger trigger, RuleCriteriaContext context);
}
