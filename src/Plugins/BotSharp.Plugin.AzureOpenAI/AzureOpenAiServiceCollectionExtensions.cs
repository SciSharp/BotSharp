using BotSharp.Abstraction.Infrastructures.ContentTransfers;
using BotSharp.Platform.AzureAi;
using BotSharp.Plugin.AzureOpenAI.TextGeneratives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class AzureOpenAiServiceCollectionExtensions
{
    public static IServiceCollection AddAzureOpenAiPlatform(this IServiceCollection services, IConfiguration config)
    {
        var settings = new AzureOpenAiSettings();
        config.Bind("AzureOpenAi", settings);

        services.AddSingleton(x =>
        {
            return settings;
        });

        services.AddScoped<IServiceZone, ChatCompletionProvider>();

        return services;
    }
}