using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.GoogleAI.Providers;
using BotSharp.Plugin.GoogleAI.Settings;

namespace BotSharp.Plugin.GoogleAI;

public class GoogleAiPlugin : IBotSharpPlugin
{
    public string Name => "Google AI";
    public string Description => "Making AI helpful for everyone (PaLM 2, Gemini)";
    public string IconUrl => "https://vectorseek.com/wp-content/uploads/2021/12/Google-AI-Logo-Vector.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new GoogleAiSettings();
        config.Bind("GoogleAi", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded Google AI settings: {settings.PaLM.Endpoint} {settings.PaLM.ApiKey.SubstringMax(4)}");
            return settings;
        });

        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
    }
}
