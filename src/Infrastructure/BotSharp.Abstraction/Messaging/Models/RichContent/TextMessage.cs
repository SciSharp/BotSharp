namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class TextMessage : IRichMessage
{
    public string Text { get; set; } = string.Empty;

    public TextMessage(string text)
    {
        Text = text;
    }
}