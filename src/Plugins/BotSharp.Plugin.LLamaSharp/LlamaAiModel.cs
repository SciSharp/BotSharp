using BotSharp.Plugin.LLamaSharp.Settings;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using System.IO;

namespace BotSharp.Plugins.LLamaSharp;

public class LlamaAiModel
{
    private readonly LlamaSharpSettings _settings;
    public LlamaSharpSettings Settings => _settings;

    LLamaWeights _model;

    public LLamaWeights Model => _model;

    ModelParams _params;
    public ModelParams Params => _params;

    private ILLamaExecutor _statelessExecutor;

    public LlamaAiModel(LlamaSharpSettings settings)
    {
        _settings = settings;
    }


    public void LoadModel(string model)
    {
        if (_model != null)
        {
            return;
        }

        _params = new ModelParams(Path.Combine(_settings.ModelDir, model))
        {
            ContextSize = (uint)_settings.MaxContextLength,
            GpuLayerCount = _settings.NumberOfGpuLayer
        };

        _model = LLamaWeights.LoadFromFile(_params);
    }

    public ILLamaExecutor GetStatelessExecutor()
    {
        if (_statelessExecutor == null)
        {
            _statelessExecutor = new StatelessExecutor(_model, _params);
        }
        return _statelessExecutor;
    }
}
