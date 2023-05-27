using BotSharp.Abstraction;
using BotSharp.Platform.LlamaSharp;
using LLama;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Local.Handlers;

public class ChatCompletionHandler : IChatCompletionHandler
{
    private readonly IChatModel _model;
    private readonly LlamaSharpSettings _settings;

    public ChatCompletionHandler(LlamaSharpSettings settings)
    {
        _settings = settings;
        _model = new LLamaModel(new LLamaParams(model: _settings.ModelPath, 
            n_ctx: 512, 
            interactive: true, 
            repeat_penalty: 1.0f, 
            verbose_prompt: false));

        if (!string.IsNullOrEmpty(settings.InstructionFile))
        {
            var prompt = File.ReadAllText(settings.InstructionFile);
            _model.InitChatPrompt(prompt, "UTF-8");
        }

        _model.InitChatAntiprompt(new string[] { "User:" });
    }

    public Task GetChatCompletionsAsync(string text, Func<string, bool, Task> onChunkReceived)
    {
        string totalResponse = "";
        foreach (var response in _model.Chat(text, "", "UTF-8"))
        {
            Console.WriteLine(response);
            totalResponse += response;
            onChunkReceived(response, false);
        }
        onChunkReceived("", true);
        return Task.CompletedTask;
    }
}
