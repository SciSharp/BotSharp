using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.GoogleAI.Providers;
using BotSharp.Plugin.GoogleAI.Settings;

namespace BotSharp.Plugin.GoogleAI;

public class GoogleAiPlugin : IBotSharpPlugin
{
    public string Id => "962ff441-2b40-4db4-b530-49efb1688a75";
    public string Name => "Google AI";
    public string Description => "Making AI helpful for everyone (PaLM 2, Gemini)";
    public string IconUrl => "https://vectorseek.com/wp-content/uploads/2021/12/Google-AI-Logo-Vector.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<GoogleAiSettings>("GoogleAi");
        });

        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
    }
}
