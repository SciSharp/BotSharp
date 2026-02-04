namespace BotSharp.Abstraction.Rules.Models;

/// <summary>
/// Result of a rule action execution
/// </summary>
public class RuleActionResult
{
    /// <summary>
    /// Whether the action executed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The conversation ID if a new conversation was created
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Response content from the action
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    public static RuleActionResult Succeeded(string? response = null)
    {
        return new RuleActionResult
        {
            Success = true,
            Response = response
        };
    }

    public static RuleActionResult Failed(string errorMessage)
    {
        return new RuleActionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

