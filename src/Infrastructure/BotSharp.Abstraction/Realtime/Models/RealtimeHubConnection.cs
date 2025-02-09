namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeHubConnection
{
    public string Event { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public string Data { get; set; } = string.Empty;
    public string Model { get; set; } = null!;
    public Func<string, object> OnModelMessageReceived { get; set; } = null!;
    public Func<object> OnModelAudioResponseDone { get; set; } = null!;
    public Func<object> OnModelUserInterrupted { get; set; } = null!;
}
