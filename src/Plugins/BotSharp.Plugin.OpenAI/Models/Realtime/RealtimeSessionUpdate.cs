namespace BotSharp.Plugin.OpenAI.Models.Realtime;

public class RealtimeSessionUpdate
{
    /// <summary>
    /// Optional client-generated ID used to identify this event.
    /// </summary>
    public string EventId { get; set; } = null!;
    public string Type { get; set; } = "session.update";
    public RealtimeSession Session { get; set; } = null!;
}
