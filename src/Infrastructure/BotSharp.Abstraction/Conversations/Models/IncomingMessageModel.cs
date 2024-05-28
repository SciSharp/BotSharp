namespace BotSharp.Abstraction.Conversations.Models;

public class IncomingMessageModel : MessageConfig
{
    public string Text { get; set; } = string.Empty;
    public virtual string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Postback message
    /// </summary>
    public PostbackMessageModel? Postback { get; set; }

    public List<BotSharpFile> Files { get; set; } = new List<BotSharpFile>();
}
