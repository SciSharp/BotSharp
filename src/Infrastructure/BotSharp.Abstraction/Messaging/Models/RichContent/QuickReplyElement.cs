using Newtonsoft.Json;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class QuickReplyElement
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "text";

    public string Title { get; set; } = string.Empty;
    public string? Payload { get; set; }

    [JsonPropertyName("image_url")]
    [JsonProperty("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
}
