using BotSharp.Abstraction.MLTasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Plugin.SemanticKernel
{
    /// <summary>
    /// Use Semantic Kernel Memory as text embedding provider
    /// </summary>
    public class SemanticKernelTextEmbeddingProvider : ITextEmbedding
    {
        private readonly ITextEmbeddingGeneration _embedding;

        /// <summary>
        /// Constructor of <see cref="SemanticKernelTextEmbeddingProvider"/>
        /// </summary>
        /// <param name="kernel"></param>
        public SemanticKernelTextEmbeddingProvider(ITextEmbeddingGeneration embedding, int dimension)
        {
            this._embedding = embedding;
            Dimension = dimension;
        }

        public int Dimension { get; }

        public float[] GetVector(string text)
        {
            return this._embedding.GenerateEmbeddingAsync(text)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToArray();
        }

        public List<float[]> GetVectors(List<string> texts)
        {
            return this._embedding.GenerateEmbeddingsAsync(texts).ConfigureAwait(false).GetAwaiter().GetResult()
                .Select(_ => _.ToArray())
                .ToList();
        }
    }
}
