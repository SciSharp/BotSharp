using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.MetaAI.Providers;
using BotSharp.Plugin.MetaAI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.MetaAI;

public class MetaAiPlugin : IBotSharpPlugin
{
    public string Id => "5b18ca9b-82ed-494b-b121-e3499661edb0";
    public string Name => "Meta AI";
    public string Description => "Innovating with the freedom to explore, discover and apply AI at scale.";
    public string IconUrl => "https://static.xx.fbcdn.net/rsrc.php/yJ/r/C1E_YZIckM5.svg";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<MetaAiSettings>("MetaAi");
        });

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<MetaAiSettings>("MetaAi").fastText;
        });

        services.AddSingleton<ITextEmbedding, fastTextEmbeddingProvider>();
    }
}
