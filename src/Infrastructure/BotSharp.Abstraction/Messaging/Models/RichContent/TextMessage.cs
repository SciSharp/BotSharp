namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class TextMessage : IRichMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => "text";

    public string Text { get; set; } = string.Empty;

    public TextMessage(string text)
    {
        Text = text;
    }
}