using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class TextMessage : IRichMessage
{
    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    public string RichType => RichTypeEnum.Text;

    [Translate]
    public string Text { get; set; } = string.Empty;

    public TextMessage(string text)
    {
        Text = text;
    }
}