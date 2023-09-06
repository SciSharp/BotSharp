namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel
{
    public string Text { get; set; }

    /// <summary>
    /// Conversation states from input
    /// </summary>
    public List<string> States { get; set; } = new List<string>();
}
