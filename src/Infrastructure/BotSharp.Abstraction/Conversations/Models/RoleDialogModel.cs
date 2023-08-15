namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel
{
    /// <summary>
    /// user, system, assistant, function
    /// </summary>
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Content { get; set; }

    /// <summary>
    /// Function name if LLM response function call
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Function execution result
    /// </summary>
    public string? ExecutionResult { get; set; }

    /// <summary>
    /// When function callback has been executed, system will pass result to LLM again,
    /// Set this property to True to stop calling LLM.
    /// </summary>
    public bool StopSubsequentInteraction { get; set; }

    public bool IsConversationEnd { get; set; }

    /// <summary>
    /// Channel name
    /// </summary>
    public string Channel { get; set; }

    public RoleDialogModel(string role, string text)
    {
        Role = role;
        Content = text;
    }

    public override string ToString()
    {
        return $"{Role}: {Content}";
    }
}
