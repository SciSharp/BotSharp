namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel
{
    public string Text { get; set; }
    public string ModelName { get; set; } = "gpt-3.5-turbo";
    public string Channel { get; set; } = "openapi";

    /// <summary>
    /// Conversation states from input
    /// </summary>
    public List<string> States { get; set; } = new List<string>();
}
