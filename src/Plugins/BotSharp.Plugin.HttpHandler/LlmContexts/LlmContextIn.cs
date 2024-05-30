using System.Text.Json.Serialization;

namespace BotSharp.Plugin.HttpHandler.LlmContexts;

public class LlmContextIn
{
    [JsonPropertyName("request_url")]
    public string? RequestUrl { get; set; }

    [JsonPropertyName("http_method")]
    public string? HttpMethod { get; set; }

    [JsonPropertyName("request_content")]
    public string? RequestContent { get; set; }
}
