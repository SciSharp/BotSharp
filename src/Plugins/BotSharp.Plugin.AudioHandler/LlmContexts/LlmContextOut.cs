using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
