namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class TextMessage : RichMessageBase, IRichMessage
{
    public string Type => "text";

    public TextMessage(string text)
    {
        Text = text;
    }
}