using System.Text.Json.Serialization;

namespace BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.LlmContexts;

public class HangupPhoneCallArgs
{
    [JsonPropertyName("anything_else_to_help")]
    public bool AnythingElseToHelp { get; set; } = true;
}
