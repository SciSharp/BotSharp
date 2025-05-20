namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeModelSettings
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4o-mini-realtime-preview";
    public string[] Modalities { get; set; } = ["text", "audio"];
    public bool InterruptResponse { get; set; } = true;
    public string InputAudioFormat { get; set; } = "g711_ulaw";
    public string OutputAudioFormat { get; set; } = "g711_ulaw";
    public bool InputAudioTranscribe { get; set; } = false;
    public string Voice { get; set; } = "alloy";
    public float Temperature { get; set; } = 0.8f;
    public int MaxResponseOutputTokens { get; set; } = 512;
    public int ModelResponseTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether the target event arrives after ModelResponseTimeoutSeconds, e.g., "response.done"
    /// </summary>
    public string? ModelResponseTimeoutEndEvent { get; set; }
    public AudioTranscription InputAudioTranscription { get; set; } = new();
    public ModelTurnDetection TurnDetection { get; set; } = new();
}
