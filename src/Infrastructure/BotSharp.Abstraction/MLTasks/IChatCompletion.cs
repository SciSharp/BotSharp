using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    Task<string> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations);
}
