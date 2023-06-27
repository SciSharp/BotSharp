namespace BotSharp.Abstraction.Conversations.Models;

public class RoleDialogModel
{
    /// <summary>
    /// user, system, assistant
    /// </summary>
    public string Role { get; set; }
    public string Text { get; set; }

    public override string ToString()
    {
        return $"{Role}: {Text}";
    }
}
