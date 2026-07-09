using BotSharp.Plugin.MiniMaxAI.Providers.Chat;
using BotSharp.Plugin.MiniMaxAI.Providers.Text;

namespace BotSharp.Plugin.MiniMaxAI;

public class MiniMaxAiPlugin : IBotSharpPlugin
{
    public string Id => "8a4ebd68-4d7d-4c5c-aed9-263946cc3a0d";
    public string Name => "MiniMax";
    public string Description => "MiniMax AI models exposed through the OpenAI-compatible API.";
    public string IconUrl => "https://www.minimax.io/favicon.ico";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
