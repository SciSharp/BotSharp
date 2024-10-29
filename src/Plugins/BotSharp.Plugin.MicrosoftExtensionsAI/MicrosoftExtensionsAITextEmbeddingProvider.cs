using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MicrosoftExtensionsAI;

/// <summary>
/// Provides an implementation of <see cref="ITextEmbedding"/> for Microsoft.Extensions.AI.
/// </summary>
public sealed class MicrosoftExtensionsAITextEmbeddingProvider : ITextEmbedding
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private string? _model;
    private int? _dimensions;

    /// <summary>
    /// Creates an instance of the <see cref="MicrosoftExtensionsAITextEmbeddingProvider"/> class.
    /// </summary>
    public MicrosoftExtensionsAITextEmbeddingProvider(IEmbeddingGenerator<string, Embedding<float>> generator) => 
        _generator = generator;

    /// <inheritdoc/>
    public string Provider => "microsoft-extensions-ai";

    /// <inheritdoc/>
    public async Task<float[]> GetVectorAsync(string text) =>
        (await _generator.GenerateEmbeddingVectorAsync(text, CreateOptions())).ToArray();

    /// <inheritdoc/>
    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        var embeddings = await _generator.GenerateAsync(texts, CreateOptions());
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }

    /// <inheritdoc/>
    public void SetModelName(string model) => _model = model;

    /// <inheritdoc/>
    public void SetDimension(int dimension)
    {
        if (dimension > 0)
        {
            _dimensions = dimension;
        }
    }

    /// <inheritdoc/>
    public int GetDimension() => _dimensions ?? 0;

    private EmbeddingGenerationOptions CreateOptions() =>
        new()
        {
            ModelId = _model,
            Dimensions = _dimensions,
        };
}
