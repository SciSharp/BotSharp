using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.AnthropicAI.Providers;
using BotSharp.Plugin.AnthropicAI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.AnthropicAI;

public class AnthropicPlugin : IBotSharpPlugin
{
    public string Id => "012119da-8367-4be8-9a75-ab6ae55071e6";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new AnthropicSettings();
        config.Bind("Anthropic", settings);
        services.AddSingleton(x =>
        {
            // Console.WriteLine($"Loaded Anthropic settings: {settings.Claude.ApiKey.SubstringMax(4)}");
            return settings;
        });

        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        // services.AddScoped<ITextCompletion, TextCompletionProvider>();
    }
}
