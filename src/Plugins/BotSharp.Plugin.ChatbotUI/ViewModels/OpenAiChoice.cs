using BotSharp.Abstraction.Conversations.Models;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ChatbotUI.ViewModels;

public class OpenAiChoice
{
    [JsonPropertyName("finish_reason")]
    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
    public RoleDialogModel Delta { get; set; }
}
