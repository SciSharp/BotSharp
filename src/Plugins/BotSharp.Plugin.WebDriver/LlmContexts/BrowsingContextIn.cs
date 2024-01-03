using System.Text.Json.Serialization;

namespace BotSharp.Plugin.WebDriver.LlmContexts;

public class BrowsingContextIn
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("element_name")]
    public string? ElementName { get; set; }

    [JsonPropertyName("input_text")]
    public string? InputText { get; set; }
}
