using BotSharp.Abstraction;
using BotSharp.Platform.Native.Handlers;
using BotSharp.Platform.Native.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class BotSharplServiceCollectionExtensions
{
    public static IServiceCollection AddBotSharpCommunityPlatform(this IServiceCollection services, IConfiguration config)
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