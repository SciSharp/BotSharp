namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeModelSettings
{
    public bool InterruptResponse { get; set; } = false;
    public string InputAudioFormat { get; set; } = "g711_ulaw";
    public string OutputAudioFormat { get; set; } = "g711_ulaw";
    public string Voice { get; set; } = "alloy";
    public float Temperature { get; set; } = 0.8f;
    public int MaxResponseOutputTokens { get; set; } = 512;
    public AudioTranscription InputAudioTranscription { get; set; } = new();
    public ModelTurnDetection TurnDetection { get; set; } = new();
}
