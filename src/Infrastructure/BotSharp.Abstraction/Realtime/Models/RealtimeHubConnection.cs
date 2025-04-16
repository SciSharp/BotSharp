namespace BotSharp.Abstraction.Realtime.Models;

public class RealtimeHubConnection
{
    public string StreamId { get; set; } = null!;
    public string? LastAssistantItemId { get; set; } = null!;
    public long LatestMediaTimestamp { get; set; }
    public long? ResponseStartTimestamp { get; set; }
    public string KeypadInputBuffer { get; set; } = string.Empty;
    public string CurrentAgentId { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public Func<string> OnModelReady { get; set; } = () => string.Empty;
    public Func<string, string> OnModelMessageReceived { get; set; } = null!;
    public Func<string> OnModelAudioResponseDone { get; set; } = null!;
    public Func<string> OnModelUserInterrupted { get; set; } = null!;
    public Func<string> OnUserSpeechDetected { get; set; } = () => string.Empty;

    public void ResetResponseState()
    {
        LastAssistantItemId = null;
        ResponseStartTimestamp = null;
    }

    public void ResetStreamState()
    {
        ResponseStartTimestamp = null;
        LatestMediaTimestamp = 0;
    }
}
