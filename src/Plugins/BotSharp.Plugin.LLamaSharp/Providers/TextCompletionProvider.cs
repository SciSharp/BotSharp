using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.LLamaSharp.Settings;
using BotSharp.Plugins.LLamaSharp;
using LLama;
using LLama.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly LlamaSharpSettings _settings;

    public TextCompletionProvider(IServiceProvider services,
        LlamaSharpSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public Task<string> GetCompletion(string text)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var model = state.GetState("model", _settings.DefaultModel);

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(model);

        var executor = new InstructExecutor(llama.Model.CreateContext(llama.Params));
        var inferenceParams = new InferenceParams() { Temperature = 0.5f, MaxTokens = 128 };

        string totalResponse = "";
        foreach (var response in executor.Infer(text, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        return Task.FromResult(totalResponse);
    }
}
