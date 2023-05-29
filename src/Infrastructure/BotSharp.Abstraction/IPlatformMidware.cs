using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction;

public interface IPlatformMidware
{
    Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived);
}
