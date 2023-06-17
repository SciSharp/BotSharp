using BotSharp.Abstraction.MLTasks;
using FastText.NetWrapper;

namespace BotSharp.Core.Plugins.fastText;

public class fastTextEmbeddingProvider : ITextEmbedding
{
    FastTextWrapper fastText;
    public fastTextEmbeddingProvider()
    {
        fastText = new FastTextWrapper();

        if (!fastText.IsModelReady())
        {
            fastText.LoadModel(@"D:\Service Mesh\prediction\WebStarter\tmp_data\models\crawl-300d-2M-subword.bin");
        }
    }

    public float[] GetVector(string text)
    {
        return fastText.GetSentenceVector(text);
    }
}
