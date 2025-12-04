using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.MMPEmbedding.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.MMPEmbedding
{
    public class MMPEmbeddingPlugin : IBotSharpPlugin
    {
        public string Id => "54d04e10-fc84-493e-a8c9-39da1c83f45a";
        public string Name => "MMPEmbedding";
        public string Description => "MMP Embedding Service";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITextEmbedding, MMPEmbeddingProvider>();
        }
    }
}
