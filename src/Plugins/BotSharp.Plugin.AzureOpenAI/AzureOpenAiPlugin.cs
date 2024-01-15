using BotSharp.Abstraction.Evaluations.Settings;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.AzureOpenAI.Providers;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Platform.AzureAi;

/// <summary>
/// Azure OpenAI Service
/// </summary>
public class AzureOpenAiPlugin : IBotSharpPlugin
{
    public string Id => "65185362-392c-44fd-a023-95a198824436";
    public string Name => "Azure OpenAI";
    public string Description => "Azure OpenAI Service (ChatGPT 3.5 Turbo / 4.0)";
    public string IconUrl => "https://nanfor.com/cdn/shop/files/cursos-propios-Azure-openAI.jpg?v=1692877741";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AzureOpenAiSettings>("AzureOpenAi");
        });

        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}