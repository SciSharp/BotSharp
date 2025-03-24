namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeModelSettings
{
    public string Voice { get; set; } = "alloy";
    public float Temperature { get; set; } = 0.8f;
    public int MaxResponseOutputTokens { get; set; } = 512;
    public AudioTranscription InputAudioTranscription { get; set; } = new();
    public ModelTurnDetection TurnDetection { get; set; } = new();
}
