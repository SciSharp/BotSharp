using Newtonsoft.Json;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace BotSharp.Abstraction.Messaging.Models.RichContent;

public class ElementAction
{
    public string Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Url { get; set; }

    [JsonPropertyName("webview_height_ratio")]
    [JsonProperty("webview_height_ratio")]
    public string WebViewHeightRatio { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Payload { get; set; }
}
