using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ChatResponseModel : InstructResult
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    public UserViewModel Sender { get; set; } = new UserViewModel();

    public string? Function { get; set; }

    /// <summary>
    /// Planner instruction
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCallFromLlm? Instruction { get; set; }

    [JsonPropertyName("secondary_text")]
    public string? SecondaryText { get; set; }

    /// <summary>
    /// Rich message for UI rendering
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("rich_content")]
    public RichContent<IRichMessage>? RichContent { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("secondary_rich_content")]
    public RichContent<IRichMessage>? SecondaryRichContent { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
