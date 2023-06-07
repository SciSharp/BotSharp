using BotSharp.Abstraction.TextGeneratives;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.HuggingFace.HuggingChat.ViewModels;

public class ChatResponse
{
    public TextToken Token { get; set; }
    [JsonPropertyName("generated_text")]
    public string GeneratedText { get; set; }
    public string Details { get; set; }
}
