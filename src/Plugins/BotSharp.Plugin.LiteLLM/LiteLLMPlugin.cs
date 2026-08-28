using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.LiteLLM.Providers.Text;
using BotSharp.Plugin.LiteLLM.Providers.Chat;

namespace BotSharp.Plugin.LiteLLM;

public class LiteLLMPlugin : IBotSharpPlugin
{
    public string Id => "b3c1f0d2-6a4e-4d1b-9c8a-2f7e5a9d4b60";
    public string Name => "LiteLLM";
    public string Description => "LiteLLM AI gateway. Call 100+ LLM providers through a single OpenAI-compatible endpoint.";
    public string IconUrl => "https://litellm.ai/favicon.ico";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
