using BotSharp.Plugin.GoogleAi.Providers;
using GenerativeAI;
using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAI.Providers.Embedding;

public class TextEmbeddingProvider : ITextEmbedding
{
    protected readonly GoogleAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<TextEmbeddingProvider> _logger;

    private const int DEFAULT_DIMENSION = 1536;
    protected string _model = GoogleAIModels.TextEmbedding;
    protected int _dimension = DEFAULT_DIMENSION;

    public virtual string Provider => "google-ai";
    public string Model => _model;

    public TextEmbeddingProvider(
        GoogleAiSettings settings,
        ILogger<TextEmbeddingProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<float[]> GetVectorAsync(string text)
    {
        var client = ProviderHelper.GetGeminiClient(Provider, _model, _services);
        var embeddingClient = client.CreateEmbeddingModel(_model);
      
        var response = await embeddingClient.EmbedContentAsync(text);
        var value = response?.Embedding?.Values;
        return value.ToArray();
    }

    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        var client = ProviderHelper.GetGeminiClient(Provider, _model, _services);
        var embeddingClient = client.CreateEmbeddingModel(_model);
      
        var response = await embeddingClient.BatchEmbedContentAsync(texts.Select(s=>new Content(s, Roles.User)));
        var value = response.Embeddings;
        if (value == null)
            return new List<float[]>();
        return value.Select(x => x.Values?.ToArray()??[]).ToList();
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
}