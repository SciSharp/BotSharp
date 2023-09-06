using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase.MemVecDb;

public class MemVecDbPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IVectorDb, MemVectorDatabase>();
    }
}
