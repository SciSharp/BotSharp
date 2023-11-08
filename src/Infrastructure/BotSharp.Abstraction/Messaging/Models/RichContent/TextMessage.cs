
namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class TextMessage : IMessageTemplate
{
    public string Text { get; set; } = string.Empty;
}