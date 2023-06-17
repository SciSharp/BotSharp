using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.Plugins.fastText;

public class fastTextPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ITextEmbedding, fastTextEmbeddingProvider>();
    }
}
