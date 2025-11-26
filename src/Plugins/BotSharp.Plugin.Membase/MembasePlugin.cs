using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.Membase.Services;
using BotSharp.Plugin.Membase.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Membase;

public class MembasePlugin : IBotSharpPlugin
{
    public string Id => "8df12767-9a44-45d9-93cd-12a10adf3933";
    public string Name => "Membase";
    public string Description => "Document Database with Graph Traversal & Vector Search.";
    public string IconUrl => "https://membase.dev/favicon.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var dbSettings = new MembaseSettings();
        config.Bind("Membase", dbSettings);
        services.AddSingleton(sp => dbSettings);

        services
            .AddRefitClient<IMembaseApi>(new RefitSettings
            {
                AuthorizationHeaderValueGetter = (message, cancellation) => 
                    Task.FromResult($"Bearer {dbSettings.ApiKey}")
            })
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(dbSettings.Host));
    }
}
