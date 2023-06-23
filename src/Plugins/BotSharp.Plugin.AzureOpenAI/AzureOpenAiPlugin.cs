using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.AzureOpenAI.Providers;
using BotSharp.Plugin.AzureOpenAI.Services;
using BotSharp.Plugin.AzureOpenAI.Settings;
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
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<IChatServiceZone, ChatCompletionService>();
    }
}