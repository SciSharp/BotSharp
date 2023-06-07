using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.TextGeneratives;

public interface IChatCompletionProvider
{
    string GetInstruction();
    List<RoleDialogModel> GetChatSamples();

    Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived);
}
