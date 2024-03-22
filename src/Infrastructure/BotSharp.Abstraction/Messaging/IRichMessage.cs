using BotSharp.Abstraction.Messaging.Enums;

namespace BotSharp.Abstraction.Messaging;

public interface IRichMessage
{
    string Text { get; set; }

    [JsonPropertyName("rich_type")]
    string RichType => RichTypeEnum.Text;
}
