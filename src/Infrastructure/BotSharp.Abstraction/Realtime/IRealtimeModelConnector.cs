using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeModelConnector
{
    Task Connect(RealtimeHubConnection conn, 
        Action<string> onAudioDeltaReceived, 
        Action onAudioResponseDone, 
        Action onUserInterrupted);
    Task SendMessage(string message);
    Task Disconnect();
}
