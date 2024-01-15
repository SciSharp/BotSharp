using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.MetaAI.Settings;
using FastText.NetWrapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaAI.Providers;

public class fastTextEmbeddingProvider : ITextEmbedding
{
    private FastTextWrapper _fastText;
    private readonly IServiceProvider _services;

    public int Dimension
    {
        get
        {
            LoadModel();
            return _fastText.GetModelDimension();
        }
    }

    public fastTextEmbeddingProvider(IServiceProvider services)
    {
        _services = services;
    }

    public Task<float[]> GetVectorAsync(string text)
    {
        LoadModel();
        return Task.FromResult(_fastText.GetSentenceVector(text));
    }

    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        LoadModel();
        var vectors = new List<float[]>();
        for (int i = 0; i < texts.Count; i++)
        {
            vectors.Add(await GetVectorAsync(texts[i]));
        }
        return vectors;
    }

    private void LoadModel()
    {
        if (_fastText == null)
        {
            var settings = _services.CreateScope().ServiceProvider
                .GetRequiredService<fastTextSetting>();
            if (!File.Exists(settings.ModelPath))
            {
                throw new FileNotFoundException($"Can't load pre-trained word vectors from {settings.ModelPath}.\n Try to download from https://fasttext.cc/docs/en/english-vectors.html.");
            }

            _fastText = new FastTextWrapper();

            if (!_fastText.IsModelReady())
            {
                _fastText.LoadModel(settings.ModelPath);
            }
        }
    }
}
