namespace BotSharp.Abstraction.Messaging;

public interface IRichMessage
{
    [JsonPropertyName("text")]
    string Text { get; set; }

    [JsonPropertyName("rich_type")]
    string RichType => RichTypeEnum.Text;
}
