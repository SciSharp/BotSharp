using System.Text.RegularExpressions;
using OpenAI.Embeddings;

namespace BotSharp.Plugin.MMPEmbedding.Providers;

/// <summary>
/// Text embedding provider that uses Mean-Max Pooling strategy
/// This provider gets embeddings for individual tokens and combines them using mean and max pooling
/// </summary>
public class MMPEmbeddingProvider : ITextEmbedding
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger<MMPEmbeddingProvider> _logger;

    private const int DEFAULT_DIMENSION = 1536;
    protected string _model = "text-embedding-3-small";
    protected int _dimension = DEFAULT_DIMENSION;

    // The underlying provider to use (e.g., "openai", "azure-openai", "deepseek-ai")
    protected string _underlyingProvider = "openai";

    public string Provider => "mmp-embedding";
    public string Model => _model;

    private static readonly Regex _wordRegex = new(@"\b\w+\b", RegexOptions.Compiled);

    public MMPEmbeddingProvider(IServiceProvider serviceProvider, ILogger<MMPEmbeddingProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets a single embedding vector using mean-max pooling
    /// </summary>
    public async Task<float[]> GetVectorAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[_dimension];
        }

        var tokens = Tokenize(text).ToList();

        if (tokens.Count == 0)
        {
            return new float[_dimension];
        }

        // Get embeddings for all tokens
        var tokenEmbeddings = await GetTokenEmbeddingsAsync(tokens);

        // Apply mean-max pooling
        var pooledEmbedding = MeanMaxPooling(tokenEmbeddings);

        return pooledEmbedding;
    }

    /// <summary>
    /// Gets multiple embedding vectors using mean-max pooling
    /// </summary>
    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        var results = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = await GetVectorAsync(text);
            results.Add(embedding);
        }

        return results;
    }

    public void SetDimension(int dimension)
    {
        _dimension = dimension > 0 ? dimension : DEFAULT_DIMENSION;
    }

    public int GetDimension()
    {
        return _dimension;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    #region Private methods
    /// <summary>
    /// Sets the underlying provider to use for getting token embeddings
    /// </summary>
    /// <param name="provider">Provider name (e.g., "openai", "azure-openai", "deepseek-ai")</param>
    public void SetUnderlyingProvider(string provider)
    {
        _underlyingProvider = provider;
    }

    /// <summary>
    /// Gets embeddings for individual tokens using the underlying provider
    /// </summary>
    private async Task<List<float[]>> GetTokenEmbeddingsAsync(List<string> tokens)
    {
        try
        {
            // Get the appropriate client based on the underlying provider
            var client = ProviderHelper.GetClient(_underlyingProvider, _model, _serviceProvider);
            var embeddingClient = client.GetEmbeddingClient(_model);

            // Prepare options
            var options = new EmbeddingGenerationOptions
            {
                Dimensions = _dimension > 0 ? _dimension : null
            };

            // Get embeddings for all tokens in batch
            var response = await embeddingClient.GenerateEmbeddingsAsync(tokens, options);
            var embeddings = response.Value;

            return embeddings.Select(e => e.ToFloats().ToArray()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token embeddings from provider {Provider} with model {Model}",
                _underlyingProvider, _model);
            throw;
        }
    }

    /// <summary>
    /// Applies mean-max pooling to combine token embeddings
    /// Mean pooling: average of all token embeddings
    /// Max pooling: element-wise maximum of all token embeddings
    /// Result: concatenation of mean and max pooled vectors
    /// </summary>
    private float[] MeanMaxPooling(IReadOnlyList<float[]> vectors, float meanWeight = 0.5f, float maxWeight = 0.5f)
    {
        var numTokens = vectors.Count;

        if (numTokens == 0)
            return [];

        var meanPooled = Enumerable.Range(0, _dimension)
            .Select(i => vectors.Average(v => v[i]))
            .ToArray();
        var maxPooled = Enumerable.Range(0, _dimension)
            .Select(i => vectors.Max(v => v[i]))
            .ToArray();

        return Enumerable.Range(0, _dimension)
            .Select(i => meanWeight * meanPooled[i] + maxWeight * maxPooled[i])
            .ToArray();
    }

    /// <summary>
    /// Tokenizes text into individual words
    /// </summary>
    private static IEnumerable<string> Tokenize(string text, string? pattern = null)
    {
        var patternRegex = !string.IsNullOrEmpty(pattern) ? new(pattern, RegexOptions.Compiled) : _wordRegex;
        return patternRegex.Matches(text).Cast<Match>().Select(m => m.Value);
    }
    #endregion
}
