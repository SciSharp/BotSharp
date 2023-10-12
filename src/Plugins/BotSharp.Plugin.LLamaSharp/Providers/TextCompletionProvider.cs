using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.LLamaSharp.Settings;
using BotSharp.Plugins.LLamaSharp;
using LLama;
using LLama.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly LlamaSharpSettings _settings;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;
    public string Provider => "llama-sharp";

    public TextCompletionProvider(IServiceProvider services,
        ILogger<TextCompletionProvider> logger,
        LlamaSharpSettings settings,
        ITokenStatistics tokenStatistics)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
        _tokenStatistics = tokenStatistics;
    }

    public Task<string> GetCompletion(string text)
    {
        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(_model);

        var executor = new InstructExecutor(llama.Model.CreateContext(llama.Params));
        var inferenceParams = new InferenceParams() { Temperature = 0.5f, MaxTokens = 128 };

        _tokenStatistics.StartTimer();
        string totalResponse = "";
        foreach (var response in executor.Infer(text, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }
        _tokenStatistics.StopTimer();

        return Task.FromResult(totalResponse);
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
