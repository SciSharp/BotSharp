using System.Collections.Concurrent;

namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeHubConnection
{
    public string Event { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public string? LastAssistantItemId { get; set; } = null!;
    public long LatestMediaTimestamp { get; set; }
    public long? ResponseStartTimestamp { get; set; }
    public string KeypadInputBuffer { get; set; } = string.Empty;
    public ConcurrentQueue<string> MarkQueue { get; set; } = new();
    public string CurrentAgentId { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public string Data { get; set; } = string.Empty;
    public Func<string, object> OnModelMessageReceived { get; set; } = null!;
    public Func<object> OnModelAudioResponseDone { get; set; } = null!;
    public Func<object> OnModelUserInterrupted { get; set; } = null!;

    public void ResetResponseState()
    {
        MarkQueue.Clear();
        LastAssistantItemId = null;
        ResponseStartTimestamp = null;
    }

    public void ResetStreamState()
    {
        ResponseStartTimestamp = null;
        LatestMediaTimestamp = 0;
    }
}
