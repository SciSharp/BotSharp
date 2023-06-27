using LLama;
using LLama.Common;

namespace BotSharp.Core.Plugins.LLamaSharp;

public class LlamaAiModel
{
    private readonly LlamaSharpSettings _settings;
    public LlamaSharpSettings Settings => _settings;

    LLamaModel _model;

    public LLamaModel Model => _model;

    public LlamaAiModel(LlamaSharpSettings settings)
    {
        _settings = settings;
    }


    public void LoadModel()
    {
        if (_model != null)
        {
            return;
        }

        _model = new LLamaModel(new ModelParams(_settings.ModelPath,
            contextSize: _settings.MaxContextLength,
            gpuLayerCount: _settings.NumberOfGpuLayer));
    }
}
