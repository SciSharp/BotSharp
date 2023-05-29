using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction;

public interface IChatCompletionHandler
{
    string GetInstruction();
    List<RoleDialogModel> GetChatSamples();

    Task GetChatCompletionsAsync(List<RoleDialogModel> conversations, 
        Func<string, Task> onChunkReceived);
}
