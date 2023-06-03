using Azure.AI.OpenAI;
using System.Text.Json.Serialization;

namespace ChatbotUI.ViewModels;

public class OpenAiChoice
{
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
    public ChatMessage Delta { get; set; }
}
