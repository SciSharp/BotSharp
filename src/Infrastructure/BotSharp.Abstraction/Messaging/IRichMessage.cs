using BotSharp.Abstraction.Messaging.Enums;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging;

public interface IRichMessage
{
    [JsonPropertyName("text")]
    [JsonProperty("text")]
    string Text { get; set; }

    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    string RichType => RichTypeEnum.Text;
}
