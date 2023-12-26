using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Qdrant;

public class QdrantPlugin : IBotSharpPlugin
{
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
