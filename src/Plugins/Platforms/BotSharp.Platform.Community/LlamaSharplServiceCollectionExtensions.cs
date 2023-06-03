using BotSharp.Abstraction;
using BotSharp.Platform.Community;
using BotSharp.Platform.Community.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class LlamaSharplServiceCollectionExtensions
{
    public static IServiceCollection AddLlamaSharp(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(x =>
        {
            var settings = new LlamaSharpSettings();
            config.Bind("LlamaSharp", settings);
            return settings;
        });
        services.AddSingleton<IChatCompletionHandler, ChatCompletionHandler>();
        return services;
    }
}