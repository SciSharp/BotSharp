using BotSharp.Abstraction;
using BotSharp.Abstraction.Models;
using BotSharp.Platform.Native.Settings;
using LLama;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Native.Handlers;

public class ChatCompletionHandler : IChatCompletionHandler
{
    private readonly IChatModel _model;
    private readonly LlamaSharpSettings _settings;

    public ChatCompletionHandler(LlamaSharpSettings settings)
    {
        _settings = settings;
        _model = new LLamaModel(new LLamaParams(model: _settings.ModelPath, 
            n_ctx: _settings.MaxContextLength, 
            interactive: _settings.Interactive, 
            repeat_penalty: _settings.RepeatPenalty, 
            verbose_prompt: _settings.VerbosePrompt,
            n_gpu_layers: _settings.NumberOfGpuLayer));

        var prompt = GetInstruction();
        _model.InitChatPrompt(prompt, "UTF-8");
        _model.InitChatAntiprompt(new string[] { "user:" });
    }

    public async Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived)
    {
        string totalResponse = "";
        var prompt = GetInstruction();
        var content = string.Join(". ", conversations.Select(x => $"{x.Role}: {x.Content.Replace("user:", "")}")).Trim();
        content += ". assistant: ";
        foreach (var response in _model.Chat(content, prompt, "UTF-8"))
        {
            Console.Write(response);
            totalResponse += response;
            await onChunkReceived(response);
        }

        Console.WriteLine();
    }

    public List<RoleDialogModel> GetChatSamples()
    {
        var samples = new List<RoleDialogModel>();
        if (!string.IsNullOrEmpty(_settings.ChatSampleFile))
        {
            var lines = File.ReadAllLines(_settings.ChatSampleFile);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var role = line.Substring(0, line.IndexOf(' ') - 1);
                var content = line.Substring(line.IndexOf(' ') + 1);

                samples.Add(new RoleDialogModel
                {
                    Role = role,
                    Content = content
                });
            }
        }
        return samples;
    }

    public string GetInstruction()
    {
        var instruction = "";
        if (!string.IsNullOrEmpty(_settings.InstructionFile))
        {
            instruction = File.ReadAllText(_settings.InstructionFile);
        }

        instruction += "\n";
        foreach (var message in GetChatSamples())
        {
            instruction += $"\n{message.Role}: {message.Content}";
        }

        return instruction;
    }
}
