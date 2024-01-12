using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Qdrant;

public class QdrantPlugin : IBotSharpPlugin
{
    public string Id => "9e087f80-0f50-45bf-a87a-c1099af8f18e";
    public string Name => "Qdrant";
    public string Description => "Vector Database - Make the most of your Unstructured Data";
    public string IconUrl => "https://qdrant.tech/images/logo_with_text.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new QdrantSetting();
        config.Bind("Qdrant", settings);
        services.AddSingleton(x => settings);
        
        services.AddScoped<IVectorDb, QdrantDb>();
    }
}
