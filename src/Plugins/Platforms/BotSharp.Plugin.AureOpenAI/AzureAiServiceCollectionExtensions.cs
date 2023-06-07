using BotSharp.Abstraction.TextCompletions;
using BotSharp.Platform.AzureAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class AzureAiServiceCollectionExtensions
{
    public static IServiceCollection AddAzureOpenAiPlatform(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(x =>
        {
            var settings = new AzureAiSettings();
            config.Bind("AzureAi", settings);
            return settings;
        });
        services.AddScoped<ITextCompletionProvider, ChatCompletionHandler>();
        return services;
    }
}