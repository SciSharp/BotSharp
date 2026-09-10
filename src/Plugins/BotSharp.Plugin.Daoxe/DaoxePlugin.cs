using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.Daoxe.Providers.Text;
using BotSharp.Plugin.Daoxe.Providers.Chat;

namespace BotSharp.Plugin.Daoxe;

public class DaoxePlugin : IBotSharpPlugin
{
    public string Id => "e328c753-c84b-469c-a717-556849dac6bc";
    public string Name => "Daoxe";
    public string Description => "Daoxe (daoxe.com) AI gateway. Access hundreds of models from ~25 providers through a single OpenAI-compatible endpoint.";
    public string IconUrl => "https://daoxe.com/favicon.ico";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
