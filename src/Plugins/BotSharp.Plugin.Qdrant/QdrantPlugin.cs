using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Qdrant;

public class QdrantPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new QdrantSetting();
        config.Bind("Qdrant", settings);
        services.AddSingleton(x => settings);
        
        services.AddScoped<IVectorDb, QdrantDb>();
    }
}
