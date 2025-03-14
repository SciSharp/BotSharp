using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationSummaryModel
{
    [JsonPropertyName("conversation_ids")]
    public List<string> ConversationIds { get; set; } = new List<string>();
}
