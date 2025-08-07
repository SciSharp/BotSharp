using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeHook : IHookBase
{
    Task OnModelReady(Agent agent, IRealTimeCompletion completer)
        => Task.CompletedTask;

    string[] OnModelTranscriptPrompt(Agent agent)
        => [];

    Task OnTranscribeCompleted(RoleDialogModel message, TranscriptionData data)
        => Task.CompletedTask;

    Task<bool> ShouldReconnect(RealtimeHubConnection conn, RoleDialogModel message) 
        => Task.FromResult(false);
}
