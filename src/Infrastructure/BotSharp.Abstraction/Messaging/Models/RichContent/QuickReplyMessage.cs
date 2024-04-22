using BotSharp.Abstraction.Messaging.Enums;
using Newtonsoft.Json;

namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class QuickReplyMessage : IRichMessage
{
    [JsonPropertyName("rich_type")]
    [JsonProperty("rich_type")]
    public string RichType => RichTypeEnum.QuickReply;
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("quick_replies")]
    [JsonProperty("quick_replies")]
    public List<QuickReplyElement> QuickReplies { get; set; } = new List<QuickReplyElement>();
}
