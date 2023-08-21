using BotSharp.Plugin.MetaMessenger.Interfaces;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/templates
/// </summary>
public class TemplateMessage : IResponseMessage
{
    [JsonPropertyName("attachment")]
    public AttachmentBody Attachment { get; set; }
}
