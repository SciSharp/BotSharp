namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeModelSettings
{
    public float Temperature { get; set; } = 0.6f;
    public int MaxResponseOutputTokens { get; set; } = 512;
    public ModelTurnDetection TurnDetection { get; set; } = new();
}
