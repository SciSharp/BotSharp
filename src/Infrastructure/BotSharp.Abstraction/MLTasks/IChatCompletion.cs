using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    Task<string> GetChatCompletionsAsync(List<RoleDialogModel> conversations);
}
