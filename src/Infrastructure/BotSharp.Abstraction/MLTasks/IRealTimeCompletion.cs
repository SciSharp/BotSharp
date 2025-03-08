using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IRealTimeCompletion
{
    string Provider { get; }
    string Model { get; }
    void SetModelName(string model);

    Task Connect(RealtimeHubConnection conn,
        Action onModelReady,
        Action<string, string> onModelAudioDeltaReceived,
        Action onModelAudioResponseDone,
        Action<string> onAudioTranscriptDone,
        Action<List<RoleDialogModel>> onModelResponseDone,
        Action<string> onConversationItemCreated,
        Action<RoleDialogModel> onInputAudioTranscriptionCompleted,
        Action onUserInterrupted);
    Task AppenAudioBuffer(string message);

    Task SendEventToModel(object message);
    Task Disconnect();

    Task<RealtimeSession> CreateSession(Agent agent, List<RoleDialogModel> conversations);
    Task<string> UpdateSession(RealtimeHubConnection conn, bool turnDetection = true);
    Task InsertConversationItem(RoleDialogModel message);
    Task RemoveConversationItem(string itemId);
    Task TriggerModelInference(string? instructions = null);
    Task CancelModelResponse();
    Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response);
    Task<RoleDialogModel> OnConversationItemCreated(RealtimeHubConnection conn, string response);
}
