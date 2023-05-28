using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction;

public interface IChatCompletionHandler
{
    Task GetChatCompletionsAsync(string text, 
        Func<string> GetInstruction, 
        Func<List<RoleDialogModel>> GetChatHistory,
        Func<string, Task> onChunkReceived,
        Func<Task> onChunkCompleted);
}
