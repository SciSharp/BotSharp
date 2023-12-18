namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/templates
/// </summary>
public class AttachmentMessage : IRichMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => "attachment";

    [JsonIgnore]
    public string Text { get; set; }

    [JsonPropertyName("attachment")]
    public AttachmentBody Attachment { get; set; }
}
