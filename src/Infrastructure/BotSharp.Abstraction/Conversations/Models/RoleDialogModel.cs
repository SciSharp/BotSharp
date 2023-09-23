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
    /// Function execution result, this result will be seen by LLM.
    /// </summary>
    public string? ExecutionResult { get; set; }

    /// <summary>
    /// Function execution structured data, this data won't pass to LLM.
    /// It's ideal to render in rich content in UI.
    /// </summary>
    public object ExecutionData { get; set; }

    /// <summary>
    /// Intent name
    /// </summary>
    public string IntentName {  get; set; }

    /// <summary>
    /// Stop conversation completion
    /// </summary>
    public bool StopCompletion { get; set; }

    public RoleDialogModel(string role, string text)
    {
        Role = role;
        Content = text;
    }

    public override string ToString()
    {
        if (Role == AgentRole.Function)
        {
            return $"{Role}: {FunctionName} => {ExecutionResult}";
        }
        else
        {
            return $"{Role}: {Content}";
        }
    }
}
