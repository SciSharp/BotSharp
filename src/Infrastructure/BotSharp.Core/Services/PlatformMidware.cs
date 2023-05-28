using BotSharp.Abstraction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.Services;

public class PlatformMidware : IPlatformMidware
{
    private readonly IServiceProvider _services;
    public PlatformMidware(IServiceProvider services)
    {
        _services = services;
    }

    public async Task GetChatCompletionsAsync(string text,
        Func<string> GetInstruction,
        Func<List<RoleDialogModel>> GetChatHistory,
        Func<string, Task> onChunkReceived,
        Func<Task> onChunkCompleted)
    {
        var handlers = _services.GetServices<IChatCompletionHandler>().ToList();
        for (int i = 0; i < handlers.Count(); i++)
        {
            var handler = handlers[i];
            await handler.GetChatCompletionsAsync(text,
                GetInstruction,
                GetChatHistory,
                onChunkReceived,
                onChunkCompleted);
        }
    }
}
