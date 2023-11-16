using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.SemanticKernel
{
    public class SemanticKernelPlugin : IBotSharpPlugin
    {
        public string Name => "Semantic Kernel";
        public string Description => "Semantic Kernel Service";

        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            var settings = new SemanticKernelSettings();
            config.Bind("SemanticKernel", settings);

            services.AddScoped<ITextCompletion, SemanticKernelTextCompletionProvider>();
            services.AddScoped<IChatCompletion, SemanticKernelChatCompletionProvider>();
            services.AddScoped<IVectorDb, SemanticKernelMemoryStoreProvider>();
            services.AddScoped<ITextEmbedding, SemanticKernelTextEmbeddingProvider>();
        }
    }
}