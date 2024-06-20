namespace BotSharp.Abstraction.Conversations.Models;

public class TruncateMessageRequest
{
    [JsonPropertyName("is_new_message")]
    public bool isNewMessage { get; set; }
}
