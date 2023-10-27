
namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class QuickReplyElement
    {
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Payload { get; set; }
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        public string? PostBackUrl { get; set; }
    }
}
