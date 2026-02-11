namespace BotSharp.Abstraction.Rules.Models;

public class RuleCriteriaResult
{
    /// <summary>
    /// Whether the criteria executed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response content from the action
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if the criteria failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
