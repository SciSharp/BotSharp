using BotSharp.Abstraction.Messaging.Enums;

namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationSenderActionModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }

    [JsonPropertyName("sender_action")]
    public SenderActionEnum SenderAction { get; set; }

    [JsonPropertyName("indication")]
    public string? Indication { get; set; }
}
