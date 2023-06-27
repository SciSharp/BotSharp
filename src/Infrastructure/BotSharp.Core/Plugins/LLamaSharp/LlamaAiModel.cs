using LLama;
namespace BotSharp.Core.Plugins.LLamaSharp;

public class LlamaAiModel
{
    private readonly LlamaSharpSettings _settings;

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

        _model = new LLamaModel(new LLamaParams(model: _settings.ModelPath,
            n_ctx: _settings.MaxContextLength,
            interactive: _settings.Interactive,
            repeat_penalty: _settings.RepeatPenalty,
            verbose_prompt: _settings.VerbosePrompt,
            n_gpu_layers: _settings.NumberOfGpuLayer));
    }
}
