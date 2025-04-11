namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeModelSettings
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4o-mini-realtime-preview";
    public bool InterruptResponse { get; set; } = true;
    public string InputAudioFormat { get; set; } = "g711_ulaw";
    public string OutputAudioFormat { get; set; } = "g711_ulaw";
    public bool InputAudioTranscribe { get; set; } = false;
    public string Voice { get; set; } = "alloy";
    public float Temperature { get; set; } = 0.8f;
    public int MaxResponseOutputTokens { get; set; } = 512;
    public int ModelResponseTimeout { get; set; } = 30;
    public AudioTranscription InputAudioTranscription { get; set; } = new();
    public ModelTurnDetection TurnDetection { get; set; } = new();
}
