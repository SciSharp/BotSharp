using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeHook : IHookBase
{
    Task OnModelReady(Agent agent, IRealTimeCompletion completer);
    string[] OnModelTranscriptPrompt(Agent agent);
    Task OnTranscribeCompleted(RoleDialogModel message, TranscriptionData data);
    Task<bool> ShouldReconnect(RealtimeHubConnection conn) => Task.FromResult(false);
}
