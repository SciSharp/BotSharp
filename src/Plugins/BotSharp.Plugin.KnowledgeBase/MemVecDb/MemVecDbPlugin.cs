using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.KnowledgeBase.MemVecDb;

public class MemVecDbPlugin : IBotSharpPlugin
{
    public string Name => "Memory Vector DB";
    public string Description => "Store text embedding, search similar text from memory.";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IVectorDb, MemVectorDatabase>();
    }
}
