using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Users.Dtos;

namespace BotSharp.Abstraction.Conversations.Dtos;

public class ChatResponseDto : InstructResult
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    public UserDto Sender { get; set; } = new UserDto();

    public string? Function { get; set; }

    /// <summary>
    /// Planner instruction
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCallFromLlm? Instruction { get; set; }

    /// <summary>
    /// Rich message for UI rendering
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("rich_content")]
    public RichContent<IRichMessage>? RichContent { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    [JsonPropertyName("has_message_files")]
    public bool HasMessageFiles { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
