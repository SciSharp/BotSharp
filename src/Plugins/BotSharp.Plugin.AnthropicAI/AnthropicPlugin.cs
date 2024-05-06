using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.AnthropicAI.Providers;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.AnthropicAI;

public class AnthropicPlugin : IBotSharpPlugin
{
    public string Id => "012119da-8367-4be8-9a75-ab6ae55071e6";
    public string Name => "Anthropic AI";
    public string Description => "Anthropic is an AI safety and research company";
    public string? IconUrl => "https://www.anthropic.com/images/icons/safari-pinned-tab.svg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new AnthropicSettings();
        config.Bind("AnthropicAi", settings);
        services.AddSingleton(x =>
        {
            // Console.WriteLine($"Loaded Anthropic settings: {settings.Claude.ApiKey.SubstringMax(4)}");
            return settings;
        });

        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        // services.AddScoped<ITextCompletion, TextCompletionProvider>();
    }
}
