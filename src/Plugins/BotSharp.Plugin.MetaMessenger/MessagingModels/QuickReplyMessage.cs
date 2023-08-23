using BotSharp.Plugin.MetaMessenger.Interfaces;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

/// <summary>
/// Quick Replies
/// https://developers.facebook.com/docs/messenger-platform/send-messages/quick-replies
/// </summary>
public class QuickReplyMessage : IResponseMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("quick_replies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QuickReplyMessageItem[]? QuickReplies { get; set; }
}
