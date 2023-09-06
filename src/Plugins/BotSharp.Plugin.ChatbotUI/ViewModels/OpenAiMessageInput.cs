using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatbotUI.ViewModels;

public class OpenAiMessageInput
{
    public string AgentId { get; set; }
    public string ConversationId { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<OpenAiMessageBody> Messages { get; set; } = new List<OpenAiMessageBody>();
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4000;
    public bool Stream { get; set; } = true;
    public float Temperature { get; set; } = 0.9f;
    /// <summary>
    /// Conversation states from input
    /// </summary>
    public List<string> States { get; set; } = new List<string>();

    public override string ToString()
    {
        return string.Join("\n", Messages.Select(x => x.Role + ": " + x.Content));
    }
}
