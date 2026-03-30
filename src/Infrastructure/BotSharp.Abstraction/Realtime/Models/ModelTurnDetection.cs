namespace BotSharp.Abstraction.Realtime.Models;

public class ModelTurnDetection
{
    public int PrefixPadding { get; set; } = 300;

    public int SilenceDuration { get; set; } = 500;

    public float Threshold { get; set; } = 0.5f;
}

public class AudioTranscription
{
    public string Model { get; set; } = Gpt4xModelConstants.GPT_4o_Mini_Transcribe;
    public string? Language { get; set; }
}
