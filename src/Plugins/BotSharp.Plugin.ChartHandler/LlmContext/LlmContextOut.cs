using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChartHandler.LlmContext;

public class LlmContextOut
{
    [JsonPropertyName("greeting_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GreetingMessage { get; set; }

    [JsonPropertyName("js_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JsCode { get; set; }
}
