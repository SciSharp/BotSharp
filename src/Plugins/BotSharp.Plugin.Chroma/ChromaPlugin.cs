using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Chroma;

public class ChromaPlugin : IBotSharpPlugin
{
    public string Id => "c6e47c66-e02c-4e0a-b9c1-5ab64d74a9a1";
    public string Name => "Chroma";
    public string Description => "Open-source search and retrieval database for AI applications";
    public string IconUrl => "https://raw.githubusercontent.com/chroma-core/chroma/main/docs/mintlify/images/light-logo.svg";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<ChromaSetting>("Chroma");
        });

        services.AddHttpClient();
        services.AddScoped<IVectorDb, ChromaDb>();
    }
}
