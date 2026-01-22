using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Rules.Models;
using BotSharp.Abstraction.Rules.Options;

namespace BotSharp.Abstraction.Rules;

/// <summary>
/// Base interface for rule actions that can be executed by the RuleEngine
/// </summary>
public interface IRuleAction
{
    /// <summary>
    /// The unique name of the rule action provider
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execute the rule action
    /// </summary>
    /// <param name="agent">The agent that triggered the rule</param>
    /// <param name="trigger">The rule trigger</param>
    /// <param name="context">The action context</param>
    /// <returns>The action execution result</returns>
    Task<RuleActionResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context);
}