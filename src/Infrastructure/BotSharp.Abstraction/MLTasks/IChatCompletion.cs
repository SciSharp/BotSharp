using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    string GetChatCompletions(Agent agent, List<RoleDialogModel> conversations);
    Task<string> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations);
}
