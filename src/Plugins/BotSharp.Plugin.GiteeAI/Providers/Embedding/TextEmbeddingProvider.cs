using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace BotSharp.Plugin.GiteeAI.Providers.Embedding;

public class TextEmbeddingProvider(
    ILogger<TextEmbeddingProvider> logger,
    IServiceProvider services) : ITextEmbedding
{
    protected readonly IServiceProvider _services = services;
    protected readonly ILogger<TextEmbeddingProvider> _logger = logger;

    private const int DEFAULT_DIMENSION = 1024;
    protected string _model = "bge-m3";

    public virtual string Provider => "gitee-ai";

    public string Model => _model;

    protected int _dimension;

    public async Task<float[]> GetVectorAsync(string text)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var embeddingClient = client.GetEmbeddingClient(_model);
        var options = PrepareOptions();
        var response = await embeddingClient.GenerateEmbeddingAsync(text, options);
        var value = response.Value;
        return value.ToFloats().ToArray();
    }

    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var embeddingClient = client.GetEmbeddingClient(_model);
        var options = PrepareOptions();
        var response = await embeddingClient.GenerateEmbeddingsAsync(texts, options);
        var value = response.Value;
        return value.Select(x => x.ToFloats().ToArray()).ToList();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private EmbeddingGenerationOptions PrepareOptions()
    {
        return new EmbeddingGenerationOptions
        {
            Dimensions = GetDimension()
        };
    }

    public int GetDimension()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var stateDimension = state.GetState("embedding_dimension");
        var defaultDimension = _dimension > 0 ? _dimension : DEFAULT_DIMENSION;

        if (int.TryParse(stateDimension, out var dimension))
        {
            return dimension > 0 ? dimension : defaultDimension;
        }
        return defaultDimension;
    }

    public void SetDimension(int dimension)
    {
        _dimension = dimension > 0 ? dimension : DEFAULT_DIMENSION;
    }
    
}