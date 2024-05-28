using BotSharp.Logger.Hooks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Translation;

public class TranslationPlugin : IBotSharpPlugin
{
    public string Id => "a81997c3-5d3a-4f18-bae0-be7a81d233ba";
    public string Name => "Multi-language Translator";
    public string Description => "Output the corresponding language response according to the user language";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IConversationHook, TranslationResponseHook>();
    }
}
