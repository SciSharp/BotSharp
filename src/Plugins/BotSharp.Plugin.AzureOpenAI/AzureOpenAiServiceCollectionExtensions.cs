using BotSharp.Abstraction.TextGeneratives;
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

        services.AddScoped<IChatCompletionProvider, ChatCompletionProvider>();

        return services;
    }
}