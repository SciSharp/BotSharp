using Refit;

namespace BotSharp.Plugin.Membase;

public class MembasePlugin : IBotSharpPlugin
{
    public string Id => "8df12767-9a44-45d9-93cd-12a10adf3933";
    public string Name => "Membase";
    public string Description => "Document Database with Graph Traversal & Vector Search.";
    public string IconUrl => "https://membase.dev/favicon.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MembaseSettings();
        config.Bind("Membase", settings);
        services.AddSingleton(sp => settings);

        services.AddTransient<MembaseAuthHandler>();
        services.AddRefitClient<IMembaseApi>(new RefitSettings
                {
                    CollectionFormat = CollectionFormat.Multi
                })
                .AddHttpMessageHandler<MembaseAuthHandler>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.Host));
    }
}
