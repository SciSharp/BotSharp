using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Plugins.MemVecDb;

public class MemVecDbPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IVectorDb, MemVectorDatabase>();
    }
}
