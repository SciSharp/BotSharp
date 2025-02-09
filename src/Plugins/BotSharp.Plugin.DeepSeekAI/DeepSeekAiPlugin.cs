using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.DeepSeek.Providers.Text;
using BotSharp.Plugin.DeepSeekAI.Providers.Chat;

namespace BotSharp.Plugin.DeepSeek;

public class DeepSeekAiPlugin : IBotSharpPlugin
{
    public string Id => "1f0e73a5-bcaa-44e9-adde-e46cd94d244b";
    public string Name => "DeepSeek";
    public string Description => "DeepSeek AI";
    public string IconUrl => "https://cdn.deepseek.com/logo.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
