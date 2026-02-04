using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Plugin.Membase.GraphDb;
using Refit;

namespace BotSharp.Plugin.Membase;

public class MembasePlugin : IBotSharpPlugin
{
    public string Id => "8df12767-9a44-45d9-93cd-12a10adf3933";
    public string Name => "Membase";
    public string Description => "Document Database with Graph Traversal & Vector Search.";
    public string IconUrl => "https://membase.dev/favicon.png";

    private string _membaseCredential = string.Empty;
    private string _membaseProjectId = string.Empty;

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

        services.AddScoped<IGraphDb, MembaseGraphDb>();

        _membaseCredential = config.GetValue<string>("Membase:ApiKey") ?? string.Empty;
        _membaseProjectId = config.GetValue<string>("Membase:ProjectId") ?? string.Empty;
    }

    public bool AttachMenu(List<PluginMenuDef> menu)
    {
        var section = menu.First(x => x.Label == "Knowledge Base");
        section?.SubMenu?.Add(new PluginMenuDef("Relationships", link: "page/knowledge-base/relationships/membase")
        {
            EmbeddingInfo = new EmbeddingData
            {
                Source = "membase",
                HtmlTag = "iframe",
                Url = $"https://console.membase.dev/query-editor/{_membaseProjectId}?token={_membaseCredential}"
            }
        });
        return true;
    }
}
