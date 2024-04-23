using Newtonsoft.Json;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace BotSharp.Abstraction.Messaging.Models.RichContent;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/send-messages/buttons
/// </summary>
public class ElementButton
{
    public string Type { get; set; } = "web_url";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Url { get; set; }

    [Translate]
    public string Title { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Payload { get; set; }

    [JsonPropertyName("is_primary")]
    [JsonProperty("is_primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("is_secondary")]
    [JsonProperty("is_secondary")]
    public bool IsSecondary { get; set; }

    [JsonPropertyName("post_action_disclaimer")]
    [JsonProperty("post_action_disclaimer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Translate]
    public string? PostActionDisclaimer { get; set; }
}
