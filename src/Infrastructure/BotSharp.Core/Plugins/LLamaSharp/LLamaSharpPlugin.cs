using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugins.LLamaSharp;

public class LLamaSharpPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var llamaSharpSettings = new LlamaSharpSettings();
        config.Bind("LlamaSharp", llamaSharpSettings);
        services.AddSingleton(x => llamaSharpSettings);

        // services.AddScoped<IServiceZone, ChatCompletionProvider>();
    }
}
