using System.Text.Json.Serialization;

namespace BotSharp.Plugin.EmailHandler.LlmContexts;

public class LlmContextOut
{
    [JsonPropertyName("selected_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<int> Selecteds { get; set; }
}
