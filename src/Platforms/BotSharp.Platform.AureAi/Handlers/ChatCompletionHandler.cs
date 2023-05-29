using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction;
using BotSharp.Abstraction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Platform.AzureAi;

public class ChatCompletionHandler : IChatCompletionHandler
{
    private readonly AzureAiSettings _settings;

    public ChatCompletionHandler(AzureAiSettings settings)
    {
        _settings = settings;
    }

    public async Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(conversations);

        var response = await client.GetChatCompletionsStreamingAsync(_settings.DeploymentModel, chatCompletionsOptions);
        using StreamingChatCompletions streaming = response.Value;

        string content = "";
        await foreach (var choice in streaming.GetChoicesStreaming())
        {
            await foreach (var message in choice.GetMessageStreaming())
            {
                if (message.Content == null)
                    continue;
                Console.Write(message.Content);
                content += message.Content;
                await onChunkReceived(message.Content);
            }
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
        if (!string.IsNullOrEmpty(_settings.InstructionFile))
        {
            return File.ReadAllText(_settings.InstructionFile);
        }
        return string.Empty;
    }

    private ChatCompletionsOptions PrepareOptions(List<RoleDialogModel> conversations)
    {
        var prompt = GetInstruction();
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, prompt)
            }
        };

        foreach (var message in GetChatSamples())
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
        }

        foreach (var message in conversations)
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
        }

        return chatCompletionsOptions;
    }
}
