using BotSharp.Abstraction.MLTasks;
using LLama;
using LLama.Common;

namespace BotSharp.Core.Plugins.LLamaSharp;

public class TextEmbeddingProvider : ITextEmbedding
{
    private readonly IServiceProvider _services;
    public int Dimension => throw new NotImplementedException();

    public TextEmbeddingProvider(IServiceProvider services)
    {
        _services = services;
    }

    public float[] GetVector(string text)
    {
        var llama = _services.GetRequiredService<LlamaAiModel>();

        var executor = new LLamaEmbedder(new ModelParams(llama.Settings.ModelPath));

        return executor.GetEmbeddings(text);
    }
}
