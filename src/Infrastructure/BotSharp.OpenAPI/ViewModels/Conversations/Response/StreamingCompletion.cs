using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

/// <summary>
/// Payload of the terminating frame. Flagged in the body rather than with an SSE event name because
/// consumers read this stream line by line and drop anything that is not a data: line.
///
/// Deliberately carries no message_id: consumers take any non-indicating frame that has one for a real
/// agent reply, and would render this one as an empty message.
/// </summary>
public sealed class StreamingCompletion
{
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    public bool Cancelled { get; set; }
}
