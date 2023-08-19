using BotSharp.Abstraction.MLTasks;
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

    public TextCompletionProvider(IServiceProvider services)
    {
        _services = services;
    }

    public Task<string> GetCompletion(string text)
    {
        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel();

        var executor = new InstructExecutor(llama.Model);
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
