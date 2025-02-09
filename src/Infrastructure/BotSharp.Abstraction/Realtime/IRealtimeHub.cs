using BotSharp.Abstraction.Realtime.Models;
using System.Net.WebSockets;

namespace BotSharp.Abstraction.Realtime;

/// <summary>
/// Realtime hub interface. Manage the WebSocket connection include User, Agent and Model.
/// </summary>
public interface IRealtimeHub
{
    Task Listen(WebSocket userWebSocket, Func<string, RealtimeHubConnection> onUserMessageReceived);
}
