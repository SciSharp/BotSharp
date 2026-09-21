using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeHook : IHookBase
{
    /// <summary>
    /// Establishes the ambient identity for a stream connection, before anything runs on it.
    ///
    /// Synchronous on purpose. Ambient identity is AsyncLocal-backed, and a write made inside an
    /// `async` method does not flow back out to its caller - so an awaited hook cannot establish
    /// identity for the connection that follows it, only for itself.
    /// </summary>
    /// <param name="userId">
    /// Who the client says it is. A browser cannot set headers on a WebSocket handshake, so this
    /// arrives as a query parameter and is UNVERIFIED: treat it as a claim to be checked, not as
    /// proof. Null when the client sent none.
    /// </param>
    void OnAuthenticate(string agentId, string conversationId, string? userId) { }

    Task OnModelReady(Agent agent, IRealTimeCompletion completer)
        => Task.CompletedTask;

    string[] OnModelTranscriptPrompt(Agent agent)
        => [];

    Task OnTranscribeCompleted(RoleDialogModel message, TranscriptionData data)
        => Task.CompletedTask;

    Task<bool> ShouldReconnect(RealtimeHubConnection conn, RoleDialogModel message) 
        => Task.FromResult(false);
}
