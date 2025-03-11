using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class LlmContextIn
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("initial_message")]
    public string? InitialMessage { get; set; }
}
