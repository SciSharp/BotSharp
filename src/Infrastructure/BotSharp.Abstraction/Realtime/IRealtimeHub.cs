using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;
using System.Net.WebSockets;

namespace BotSharp.Abstraction.Realtime;

/// <summary>
/// Realtime hub interface. Manage the WebSocket connection include User, Agent and Model.
/// </summary>
public interface IRealtimeHub
{
    RealtimeHubConnection HubConn { get; }
    RealtimeHubConnection SetHubConnection(string conversationId);

    IRealTimeCompletion Completer { get; }
    IRealTimeCompletion SetCompleter(string provider);

    Task Listen(WebSocket userWebSocket, Action<string> onUserMessageReceived);
}
