using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class HangupPhoneCallArgs
{
    [JsonPropertyName("goodbye_message")]
    public string? GoodbyeMessage { get; set; }
}
