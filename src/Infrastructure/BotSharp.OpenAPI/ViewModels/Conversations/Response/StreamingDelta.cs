using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

/// <summary>
/// One token of a streamed reply. Serializing a full ChatResponseModel per token sent 602 bytes for 5
/// characters of text, 323 of them an empty Sender repeated every token. message_id has to stay:
/// consumers reject a non-indicating frame without one.
/// </summary>
public sealed class StreamingDelta
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    public string? Function { get; set; }

    public string Text { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Thought { get; set; }
}
