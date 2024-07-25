using System.Text.Json.Serialization;

namespace BotSharp.Plugin.FileHandler.LlmContexts;

public class LlmContextOut
{
    [JsonPropertyName("selected_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Selected { get; set; }
}
