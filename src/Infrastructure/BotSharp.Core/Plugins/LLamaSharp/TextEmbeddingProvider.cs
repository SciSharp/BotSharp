using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Plugins.LLamaSharp;

public class TextEmbeddingProvider : ITextEmbedding
{
    public int Dimension => throw new NotImplementedException();
    private readonly LlamaAiModel _llama;

    public TextEmbeddingProvider(LlamaAiModel llama)
    {
        _llama = llama;
    }

    public float[] GetVector(string text)
    {
        return new float[0];
    }
}
