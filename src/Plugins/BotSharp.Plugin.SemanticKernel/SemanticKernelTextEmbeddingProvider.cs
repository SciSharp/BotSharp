using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Embeddings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// Use Semantic Kernel Memory as text embedding provider
    /// </summary>
    public class SemanticKernelTextEmbeddingProvider : ITextEmbedding
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ITextEmbeddingGenerationService _embedding;
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor of <see cref="SemanticKernelTextEmbeddingProvider"/>
        /// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public SemanticKernelTextEmbeddingProvider(ITextEmbeddingGenerationService embedding, IConfiguration configuration)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            this._embedding = embedding;
            this._configuration = configuration;
            this.Dimension = configuration.GetValue<int>("SemanticKernel:Dimension");
        }

        /// <inheritdoc/>
        public int Dimension { get; set; }

        public string Provider => "semantic-kernel";

        /// <inheritdoc/>
        public async Task<float[]> GetVectorAsync(string text)
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return (await this._embedding.GenerateEmbeddingAsync(text)).ToArray();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        /// <inheritdoc/>
        public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
        {
            var embeddings = await this._embedding.GenerateEmbeddingsAsync(texts);
            return embeddings.Select(_ => _.ToArray())
                             .ToList();
        }

        public void SetModelName(string model) { }
    }
}
