using BotSharp.Abstraction.Messaging.Models.RichContent;

namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookMessageBody
{
    [JsonPropertyName("mid")]
    public string Id { get;set; }

    [JsonPropertyName("text")]
    public string Text { get;set; }

    [JsonPropertyName("quick_reply")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QuickReplyMessage QuickReply { get;set; }
}
