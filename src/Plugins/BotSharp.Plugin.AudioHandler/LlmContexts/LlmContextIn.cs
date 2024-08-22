using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AudioHandler.LlmContexts;

public class LlmContextIn
{
    [JsonPropertyName("user_request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserRequest { get; set; }

    [JsonPropertyName("is_need_summary")]
    public bool IsNeedSummary { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; } = string.Empty;

    [JsonPropertyName("file_ids")]
    public List<string> FileIds { get; set; } = new List<string>();
}
