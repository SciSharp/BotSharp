using BotSharp.Abstraction.Realtime.Models;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BotSharp.Abstraction.MLTasks;

public interface IRealTimeCompletion
{
    string Provider { get; }
    string Model { get; }
    void SetModelName(string model);

    Task Connect(
        RealtimeHubConnection conn,
        Func<Task> onModelReady,
        Func<string, string, Task> onModelAudioDeltaReceived,
        Func<Task> onModelAudioResponseDone,
        Func<string, Task> onModelAudioTranscriptDone,
        Func<List<RoleDialogModel>, Task> onModelResponseDone,
        Func<string, Task> onConversationItemCreated,
        Func<RoleDialogModel, Task> onInputAudioTranscriptionDone,
        Func<Task> onInterruptionDetected);
    Task AppenAudioBuffer(string message);
    Task AppenAudioBuffer(ArraySegment<byte> data, int length);

    Task SendEventToModel(object message);
    Task Disconnect();

    Task<string> UpdateSession(RealtimeHubConnection conn, bool isInit = false);
    Task InsertConversationItem(RoleDialogModel message);
    Task RemoveConversationItem(string itemId);
    Task TriggerModelInference(string? instructions = null);
    Task CancelModelResponse();
    Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response);
    Task<RoleDialogModel> OnConversationItemCreated(RealtimeHubConnection conn, string response);
}
