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

    [JsonPropertyName("before_active_rounds")]
    public int? BeforeActiveRounds { get; set; }

    [JsonPropertyName("after_value")]
    public string AfterValue { get; set; }

    [JsonPropertyName("after_active_rounds")]
    public int? AfterActiveRounds { get; set; }

    [JsonPropertyName("data_type")]
    public string DataType { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("readonly")]
    public bool Readonly { get; set; }
}
