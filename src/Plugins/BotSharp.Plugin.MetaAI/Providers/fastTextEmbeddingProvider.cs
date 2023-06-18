using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.MetaAI.Settings;
using FastText.NetWrapper;
using System.IO;

namespace BotSharp.Plugin.MetaAI.Providers;

public class fastTextEmbeddingProvider : ITextEmbedding
{
    private FastTextWrapper _fastText;
    private readonly fastTextSetting _settings;

    public int Dimension => _fastText.GetModelDimension();

    public fastTextEmbeddingProvider(fastTextSetting settings)
    {
        _settings = settings;
        _fastText = new FastTextWrapper();

        if (!File.Exists(settings.ModelPath))
        {
            throw new FileNotFoundException($"Can't load pre-trained word vectors from {settings.ModelPath}.\n Try to download from https://fasttext.cc/docs/en/english-vectors.html.");
        }

        if (!_fastText.IsModelReady())
        {
            _fastText.LoadModel(_settings.ModelPath);
        }
    }

    public float[] GetVector(string text)
    {
        return _fastText.GetSentenceVector(text);
    }
}
