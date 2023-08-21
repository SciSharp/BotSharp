using System.Text.Json.Serialization;

namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public class QuickReplyMessageItem
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "text";

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }
}
