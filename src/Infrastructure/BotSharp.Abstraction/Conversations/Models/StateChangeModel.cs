namespace BotSharp.Abstraction.Conversations.Models;

public class StateChangeModel
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; }

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("before_value")]
    public string BeforeValue { get; set; }

    [JsonPropertyName("after_value")]
    public string AfterValue { get; set; }
}
