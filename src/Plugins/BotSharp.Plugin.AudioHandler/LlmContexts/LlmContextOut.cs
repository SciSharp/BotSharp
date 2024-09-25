using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AudioHandler.LlmContexts;

public class LlmContextOut
{
    [JsonPropertyName("audio_content")]
    public string AudioContent { get; set; }

    [JsonPropertyName("audio_summary")]
    public string? AudioSummary { get; set; }

    [JsonPropertyName("topic")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Topic { get; set; }
}
