using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.VectorStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// Use Semantic Kernel as BotSharp plugin
    /// </summary>
    public class SemanticKernelPlugin : IBotSharpPlugin
    {
        public string Id => "849e5ab9-276f-4cf1-8041-c089fe1a1aeb";
        /// <inheritdoc/>
        public string Name => "Semantic Kernel";

        /// <inheritdoc/>
        public string Description => "Semantic Kernel Service";

        /// <inheritdoc/>
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<ITextCompletion, SemanticKernelTextCompletionProvider>();
            services.AddScoped<IChatCompletion, SemanticKernelChatCompletionProvider>();
            services.AddScoped<IVectorDb, SemanticKernelMemoryStoreProvider>();
            services.AddScoped<ITextEmbedding, SemanticKernelTextEmbeddingProvider>();
        }
    }
}