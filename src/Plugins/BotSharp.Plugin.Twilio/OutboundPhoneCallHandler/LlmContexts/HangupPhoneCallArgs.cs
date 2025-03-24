using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class HangupPhoneCallArgs
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = null!;

    [JsonPropertyName("response_content")]
    public string ResponseContent { get; set; } = null!;
}
