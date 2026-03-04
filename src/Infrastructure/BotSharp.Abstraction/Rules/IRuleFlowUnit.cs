using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

public interface IRuleFlowUnit
{
    /// <summary>
    /// The unique name of the rule flow unit, i.e., action, condition.
    /// </summary>
    string Name => string.Empty;

    /// <summary>
    /// The agent id
    /// </summary>
    string? AgentId => null;

    /// <summary>
    /// The trigger names
    /// </summary>
    IEnumerable<string>? Triggers => null;
}
