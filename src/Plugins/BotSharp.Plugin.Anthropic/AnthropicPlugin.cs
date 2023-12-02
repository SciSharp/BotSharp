using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.Anthropic.Settings;
using BotSharp.Plugin.Anthropic.Providers;

namespace BotSharp.Plugin.Anthropic;

public class AnthropicPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new AnthropicSettings();
        config.Bind("Anthropic", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded Anthropic settings: {settings.Claude.ApiKey.SubstringMax(4)}");
            return settings;
        });

        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
    }
}
