using System.Text.Json.Serialization;

namespace BotSharp.Plugin.FileHandler.LlmContexts;

public class LlmContextIn
{
    [JsonPropertyName("user_request")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserRequest { get; set; }
}
