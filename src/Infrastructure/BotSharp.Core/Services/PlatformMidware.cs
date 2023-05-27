using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.Services;

public class PlatformMidware : IPlatformMidware
{
    private readonly IServiceProvider _services;
    public PlatformMidware(IServiceProvider services)
    {
        _services = services;
    }

    public async Task GetChatCompletionsAsync(string text, Func<string, bool, Task> onChunkReceived)
    {
        var handlers = _services.GetServices<IChatCompletionHandler>().ToList();
        for (int i = 0; i < handlers.Count(); i++)
        {
            var handler = handlers[i];
            await handler.GetChatCompletionsAsync(text, onChunkReceived);
        }
    }
}
