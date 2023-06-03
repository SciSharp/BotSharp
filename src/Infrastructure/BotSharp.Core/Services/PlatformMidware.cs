using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.Services;

public class PlatformMidware : IPlatformMidware
{
    private readonly IServiceProvider _services;
    private readonly ISessionService _sessionService;

    public PlatformMidware(IServiceProvider services)
    {
        _services = services;
        _sessionService = services.GetRequiredService<ISessionService>();
    }

    public ISessionService SessionService => _sessionService;

    public async Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived)
    {
        var handlers = _services.GetServices<IChatCompletionHandler>().ToList();
        for (int i = 0; i < handlers.Count(); i++)
        {
            var handler = handlers[i];
            await handler.GetChatCompletionsAsync(conversations,
                onChunkReceived);
        }
    }
}
