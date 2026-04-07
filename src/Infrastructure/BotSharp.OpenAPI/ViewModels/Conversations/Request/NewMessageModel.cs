using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class NewMessageModel : IncomingMessageModel
{
    public override string Channel { get; set; } = ConversationChannel.OpenAPI;

    /// <summary>
    /// Indicates whether this message uses streaming completion.
    /// When true, the streaming can be stopped via the stop endpoint.
    /// </summary>
    [JsonPropertyName("is_streaming_msg")]
    public bool IsStreamingMessage { get; set; }
}
