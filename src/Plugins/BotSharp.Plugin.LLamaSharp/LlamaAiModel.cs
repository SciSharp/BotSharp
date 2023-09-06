using BotSharp.Plugin.LLamaSharp.Settings;
using LLama;
using LLama.Abstractions;
using LLama.Common;

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

        _params = new ModelParams(_settings.ModelPath)
        {
            ContextSize = _settings.MaxContextLength,
            Seed = 1337,
            GpuLayerCount = _settings.NumberOfGpuLayer
        };
    }


    public void LoadModel()
    {
        if (_model != null)
        {
            return;
        }

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
