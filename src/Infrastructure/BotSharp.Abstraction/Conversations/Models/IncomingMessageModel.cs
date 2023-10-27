using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Conversations.Models;

public class IncomingMessageModel : MessageConfig
{
    public string Text { get; set; } = string.Empty;
    public virtual string Channel { get; set; }
}
