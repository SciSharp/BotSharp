namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel
{
    /// <summary>
    /// user, system, assistant, function
    /// </summary>
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Content { get; set; }
    public string CurrentAgentId { get; set; }

    /// <summary>
    /// Function name if LLM response function call
    /// </summary>
    public string? FunctionName { get; set; }

    public string? FunctionArgs { get; set; }

    /// <summary>
    /// Function execution result
    /// </summary>
    public string? ExecutionResult { get; set; }

    public bool IsConversationEnd { get; set; }

    public bool NeedReloadAgent { get; set; }
    public bool StopPropagate { get; set; }

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
