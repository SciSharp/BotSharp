namespace BotSharp.Abstraction.Rules.Models;

public class RuleNodeResult
{
    /// <summary>
    /// Whether the node is executed successfully
    /// </summary>
    public virtual bool Success { get; set; }

    /// <summary>
    /// Whether the node evaluation is valid (used for conditions)
    /// </summary>
    public virtual bool IsValid { get; set; }

    /// <summary>
    /// Response content from the node
    /// </summary>
    public virtual string? Response { get; set; }

    /// <summary>
    /// Error message if the node execution failed
    /// </summary>
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// Result data (used for actions)
    /// </summary>
    public virtual Dictionary<string, string> Data { get; set; } = [];

    /// <summary>
    /// Whether the node execution is delayed (used for actions)
    /// </summary>
    public virtual bool IsDelayed { get; set; }
}
