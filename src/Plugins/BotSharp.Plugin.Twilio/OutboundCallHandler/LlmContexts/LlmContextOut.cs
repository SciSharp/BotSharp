using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundCallHandler.LlmContexts
{
    public class LlmContextOut
    {
        [JsonPropertyName("conversation_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ConversationId { get; set; }
    }
}
