using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Abstraction.Realtime;

public interface IRealtimeHook
{
    Task OnModeReady(Agent agent, IRealTimeCompletion completer);
    string[] OnModelTranscriptPrompt(Agent agent);
    Task OnTranscribeCompleted(RoleDialogModel message, TranscriptionData data);
}
