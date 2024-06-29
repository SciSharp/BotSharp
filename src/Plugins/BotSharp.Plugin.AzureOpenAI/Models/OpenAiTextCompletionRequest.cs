using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AzureOpenAI.Models;

public class OpenAiTextCompletionRequest : TextCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}
