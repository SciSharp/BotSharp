using BotSharp.Abstraction.Conversations.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatbotUI.ViewModels;

public class OpenAiMessageInput : IncomingMessageModel
{
    public string AgentId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public override string Channel { get; set; } = "webchat";

    public List<OpenAiMessageBody> Messages { get; set; } = new List<OpenAiMessageBody>();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4000;

    public bool Stream { get; set; } = true;

    public override string ToString()
    {
        return string.Join("\n", Messages.Select(x => x.Role + ": " + x.Content));
    }
}
