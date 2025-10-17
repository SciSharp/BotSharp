using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Abstraction.Realtime.Options;

namespace BotSharp.Abstraction.Realtime;

/// <summary>
/// Realtime hub interface. Manage the WebSocket connection include User, Agent and Model.
/// </summary>
public interface IRealtimeHub
{
    RealtimeHubConnection HubConn { get; }
    RealtimeHubConnection SetHubConnection(string conversationId);

    IRealTimeCompletion Completer { get; }

    Task ConnectToModel(Func<string, Task>? responseToUser = null, Func<string, Task>? init = null,
        List<MessageState>? initStates = null, RealtimeOptions? options = null);
}
