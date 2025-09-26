namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeOptions
{
    [JsonPropertyName("input_audio_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InputAudioFormat { get; set; }

    [JsonPropertyName("output_audio_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OutputAudioFormat { get; set; }
}
