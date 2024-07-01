using OpenAI.Embeddings;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Embedding;

public class TextEmbeddingProvider : ITextEmbedding
{
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<TextEmbeddingProvider> _logger;

    private const int DEFAULT_DIMENSION = 1536;
    protected string _model;

    public virtual string Provider => "azure-openai";

    public int Dimension { get; set; }

    public TextEmbeddingProvider(
        AzureOpenAiSettings settings,
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

    private EmbeddingGenerationOptions PrepareOptions()
    {
        return new EmbeddingGenerationOptions
        {
            Dimensions = GetDimension()
        };
    }

    private int GetDimension()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var stateDimension = state.GetState("embedding_dimension");
        if (int.TryParse(stateDimension, out var dimension))
        {
            return dimension > 0 ? dimension :(Dimension > 0 ? Dimension: DEFAULT_DIMENSION);
        }
        return Dimension > 0 ? Dimension : DEFAULT_DIMENSION;
    }
}
