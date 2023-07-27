namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel
{
    /// <summary>
    /// user, system, assistant, function
    /// </summary>
    public string Role { get; set; }
    public string Content { get; set; }

    /// <summary>
    /// Function name if LLM response function call
    /// </summary>
    public string? Name { get; set; }

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
