using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

/// <summary>
/// Payload of the terminating frame. Flagged in the body rather than with an SSE event name because
/// consumers read this stream line by line and drop anything that is not a data: line. It carries no
/// message_id on purpose: a consumer takes any non-indicating frame that has one for a real reply.
/// </summary>
public sealed class StreamingCompletion
{
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    public bool Cancelled { get; set; }
}
