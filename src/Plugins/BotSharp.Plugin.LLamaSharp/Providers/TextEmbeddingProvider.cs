using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.LLamaSharp.Settings;
using LLama;
using LLama.Common;
using System;
using System.Collections.Generic;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextEmbeddingProvider : ITextEmbedding
{
    private LLamaEmbedder _embedder;
    private readonly LlamaSharpSettings _settings;
    private readonly IServiceProvider _services;
    public int Dimension => 4096;

    public TextEmbeddingProvider(IServiceProvider services, LlamaSharpSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public float[] GetVector(string text)
    {
        if (_embedder == null)
        {
            _embedder = new LLamaEmbedder(new ModelParams(_settings.ModelPath));
        }

        return _embedder.GetEmbeddings(text);
    }

    public List<float[]> GetVectors(List<string> texts)
    {
        throw new NotImplementedException();
    }
}
