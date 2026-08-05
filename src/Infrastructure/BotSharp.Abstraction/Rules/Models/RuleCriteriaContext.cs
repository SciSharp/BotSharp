namespace BotSharp.Abstraction.Rules.Models;

/// <summary>
/// The per-request context passed to an <see cref="IRuleCriteriaEvaluator"/>
/// to decide whether a rule should be executed.
/// </summary>
public class RuleCriteriaContext
{
    /// <summary>
    /// The trigger message text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The criteria options (evaluator type and its arguments).
    /// </summary>
    public CriteriaOptions Options { get; set; } = new();

    /// <summary>
    /// The conversation states carried with the request.
    /// </summary>
    public IEnumerable<MessageState>? States { get; set; }
}
