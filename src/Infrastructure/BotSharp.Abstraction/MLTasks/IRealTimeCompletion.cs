using BotSharp.Abstraction.Realtime.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IRealTimeCompletion
{
    string Provider { get; }

    void SetModelName(string model);

    Task<RealtimeSession> CreateSession(Agent agent, List<RoleDialogModel> conversations);
}
