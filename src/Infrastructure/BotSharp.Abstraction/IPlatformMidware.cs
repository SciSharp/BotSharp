using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction;

public interface IPlatformMidware
{
    ISessionService SessionService { get; }
    Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived);
}
