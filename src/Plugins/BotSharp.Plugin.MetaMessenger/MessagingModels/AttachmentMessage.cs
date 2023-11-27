namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/templates
/// </summary>
public class AttachmentMessage : IRichMessage
{
    [JsonIgnore]
    public string Text { get; set; }

    [JsonPropertyName("attachment")]
    public AttachmentBody Attachment { get; set; }

    public string Type => "template";
}
