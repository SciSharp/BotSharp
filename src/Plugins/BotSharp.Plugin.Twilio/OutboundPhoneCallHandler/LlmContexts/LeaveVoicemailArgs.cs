using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class LeaveVoicemailArgs
{
    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("voicemail_message")]
    public string VoicemailMessage { get; set; } = null!;
}
