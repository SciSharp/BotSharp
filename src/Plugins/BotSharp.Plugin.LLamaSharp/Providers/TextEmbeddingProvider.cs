using System.IO;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextEmbeddingProvider : ITextEmbedding
{
    private LLamaEmbedder _embedder;
    private readonly LlamaSharpSettings _settings;
    private readonly IServiceProvider _services;
    private const int DEFAULT_DIMENSION = 4096;

    protected int _dimension = DEFAULT_DIMENSION;

    public string Provider => "llama-sharp";

    public TextEmbeddingProvider(IServiceProvider services, LlamaSharpSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public Task<float[]> GetVectorAsync(string text)
    {
        if (_embedder == null)
        {
            var path = Path.Combine(_settings.ModelDir, _settings.DefaultModel);
            var @params = new ModelParams(path);
            using var weights = LLamaWeights.LoadFromFile(@params);
            _embedder = new LLamaEmbedder(weights, @params);
        }

        return _embedder.GetEmbeddings(text);
    }

    public Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model) { }

    public void SetDimension(int dimension)
    {
        _dimension = dimension > 0 ? dimension : DEFAULT_DIMENSION;
    }

    public int GetDimension()
    {
        return _dimension;
    }
}
