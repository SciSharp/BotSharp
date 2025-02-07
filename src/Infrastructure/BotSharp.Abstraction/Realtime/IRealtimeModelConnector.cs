namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeModelConnector
{
    Task Connect(Action<string> onAudioDeltaReceived, Action onAudioResponseDone, Action onUserInterrupted);
    Task SendMessage(string message);
    Task Disconnect();
}
