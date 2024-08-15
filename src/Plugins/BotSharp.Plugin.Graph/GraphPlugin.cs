using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Graph;

public class GraphPlugin : IBotSharpPlugin
{
    public string Id => "74497c25-5e8d-4ee9-b6a8-ce8fe4dabea9";
    public string Name => "Graph";
    public string Description => "Graph Database";
    public string IconUrl => "https://www.microsoft.com/en-us/research/uploads/prodnew/2024/06/GraphRag2024-BlogHeroFeature-1400x788-1.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<GraphDbSettings>("GraphDb");
        });

        services.AddScoped<IGraphDb, GraphDb>();
    }
}
