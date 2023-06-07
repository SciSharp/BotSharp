using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.TextCompletions;

public interface ITextCompletionProvider
{
    string GetInstruction();
    List<RoleDialogModel> GetChatSamples();

    Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived);
}
