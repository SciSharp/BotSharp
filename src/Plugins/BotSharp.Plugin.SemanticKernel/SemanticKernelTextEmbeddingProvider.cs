using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// Use Semantic Kernel Memory as text embedding provider
    /// </summary>
    public class SemanticKernelTextEmbeddingProvider : ITextEmbedding
    {
        private readonly ITextEmbeddingGeneration _embedding;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor of <see cref="SemanticKernelTextEmbeddingProvider"/>
        /// </summary>
        public SemanticKernelTextEmbeddingProvider(ITextEmbeddingGeneration embedding, IConfiguration configuration)
        {
            this._embedding = embedding;
            this._configuration = configuration;
            this.Dimension = configuration.GetValue<int>("SemanticKernel:Dimension");
        }

        /// <inheritdoc/>
        public int Dimension { get; set; }

        /// <inheritdoc/>
        public async Task<float[]> GetVectorAsync(string text)
        {
            return (await this._embedding.GenerateEmbeddingAsync(text)).ToArray();
        }

        /// <inheritdoc/>
        public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
        {
            var embeddings = await this._embedding.GenerateEmbeddingsAsync(texts);
            return embeddings.Select(_ => _.ToArray())
                             .ToList();
        }
    }
}
