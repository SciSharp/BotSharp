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
        /// <inheritdoc/>
        public string Name => "Semantic Kernel";

        /// <inheritdoc/>
        public string Description => "Semantic Kernel Service";

        /// <inheritdoc/>
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {

            var provider = services.BuildServiceProvider().CreateScope().ServiceProvider;

            if (provider.GetService<Microsoft.SemanticKernel.AI.TextCompletion.ITextCompletion>() != null)
            {
                services.AddScoped<ITextCompletion, SemanticKernelTextCompletionProvider>();
            }

            if (provider.GetService<Microsoft.SemanticKernel.AI.ChatCompletion.IChatCompletion>() != null)
            {
                services.AddScoped<IChatCompletion, SemanticKernelChatCompletionProvider>();
            }

            if (provider.GetService<Microsoft.SemanticKernel.Memory.IMemoryStore>() != null)
            {
                services.AddScoped<IVectorDb, SemanticKernelMemoryStoreProvider>();
            }

            if (provider.GetService<Microsoft.SemanticKernel.AI.Embeddings.ITextEmbeddingGeneration>() != null)
            {
                services.AddScoped<ITextEmbedding, SemanticKernelTextEmbeddingProvider>();
            }
        }
    }
}