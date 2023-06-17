using BotSharp.Abstraction.Infrastructures.ContentTransfers;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.AzureOpenAI.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Platform.AzureAi;

public class AzureOpenAiPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new AzureOpenAiSettings();
        config.Bind("AzureOpenAi", settings);
        services.AddSingleton(x => settings);

        services.AddSingleton<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IServiceZone, ChatCompletionProvider>();
    }
}