using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.OpenAPI.ViewModels.Users;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ChatResponseModel : InstructResult
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }

    public UserViewModel Sender { get; set; }

    public string Function { get; set; }

    /// <summary>
    /// Planner instruction
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCallFromLlm Instruction { get; set; }

    /// <summary>
    /// Rich message for UI rendering
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("rich_content")]
    public object? RichContent { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
