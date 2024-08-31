using OpenAI.Embeddings;

namespace BotSharp.Plugin.OpenAI.Providers.Embedding;

public class TextEmbeddingProvider : ITextEmbedding
{
    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<TextEmbeddingProvider> _logger;

    private const int DEFAULT_DIMENSION = 3072;
    protected string _model = "text-embedding-3-large";
    protected int _dimension = DEFAULT_DIMENSION;

    public virtual string Provider => "openai";

    public TextEmbeddingProvider(
        OpenAiSettings settings,
        ILogger<TextEmbeddingProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<float[]> GetVectorAsync(string text)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var embeddingClient = client.GetEmbeddingClient(_model);
        var options = PrepareOptions();
        var response = await embeddingClient.GenerateEmbeddingAsync(text, options);
        var value = response.Value;
        return value.Vector.ToArray();
    }

    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var embeddingClient = client.GetEmbeddingClient(_model);
        var options = PrepareOptions();
        var response = await embeddingClient.GenerateEmbeddingsAsync(texts, options);
        var value = response.Value;
        return value.Select(x => x.Vector.ToArray()).ToList();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public void SetDimension(int dimension)
    {
        _dimension = dimension > 0 ? dimension : DEFAULT_DIMENSION;
    }

    public int GetDimension()
    {
        return _dimension;
    }

    private EmbeddingGenerationOptions PrepareOptions()
    {
        return new EmbeddingGenerationOptions
        {
            Dimensions = GetDimensionOption()
        };
    }

    private int GetDimensionOption()
    {
        return _dimension > 0 ? _dimension : DEFAULT_DIMENSION;
    }
}