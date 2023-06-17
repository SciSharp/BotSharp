using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.MetaAI.Settings;
using FastText.NetWrapper;

namespace BotSharp.Plugin.MetaAI.Providers;

public class fastTextEmbeddingProvider : ITextEmbedding
{
    private FastTextWrapper _fastText;
    private readonly fastTextSetting _settings;

    public fastTextEmbeddingProvider(fastTextSetting settings)
    {
        _settings = settings;
        _fastText = new FastTextWrapper();

        if (!_fastText.IsModelReady())
        {
            _fastText.LoadModel(settings.ModelPath);
        }
    }

    public float[] GetVector(string text)
    {
        return _fastText.GetSentenceVector(text);
    }
}
