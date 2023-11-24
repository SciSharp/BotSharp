namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

public class SendingMessageResponse
{
    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; }

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
}
