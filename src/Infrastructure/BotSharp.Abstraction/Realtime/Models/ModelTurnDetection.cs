namespace BotSharp.Abstraction.Realtime.Models;

public class ModelTurnDetection
{
    public int PrefixPadding { get; set; } = 300;

    public int SilenceDuration { get; set; } = 800;

    public float Threshold { get; set; } = 0.8f;
}
