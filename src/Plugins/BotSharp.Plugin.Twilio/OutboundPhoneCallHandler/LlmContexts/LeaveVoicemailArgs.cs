using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class LeaveVoicemailArgs
{
    [JsonPropertyName("voicemail_message")]
    public string VoicemailMessage { get; set; } = null!;
}
