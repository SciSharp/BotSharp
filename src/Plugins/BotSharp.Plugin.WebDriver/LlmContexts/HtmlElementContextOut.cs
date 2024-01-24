using System.Text.Json.Serialization;

namespace BotSharp.Plugin.WebDriver.LlmContexts;

public class HtmlElementContextOut
{
    [JsonPropertyName("element_id")]
    public string ElementId { get; set; }

    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}
