using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAI.Models.Realtime;

internal class RealtimeClientPayload
{
    [JsonPropertyName("setup")]
    public RealtimeGenerateContentSetup? Setup { get; set; }

    [JsonPropertyName("clientContent")]
    public BidiGenerateContentClientContent? ClientContent { get; set; }

    [JsonPropertyName("realtimeInput")]
    public BidiGenerateContentRealtimeInput? RealtimeInput { get; set; }

    [JsonPropertyName("toolResponse")]
    public BidiGenerateContentToolResponse? ToolResponse { get; set; }
}
