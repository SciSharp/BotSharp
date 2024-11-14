using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundCallHandler.LlmContexts
{
    public class LlmContextIn
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("first_message")]
        public string FirstMessage { get; set; }
    }
}
