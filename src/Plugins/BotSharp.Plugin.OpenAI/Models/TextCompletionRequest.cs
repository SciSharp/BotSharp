using System.Text.Json.Serialization;

namespace BotSharp.Plugin.OpenAI.Models;

public class TextCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 256;

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0;
}
