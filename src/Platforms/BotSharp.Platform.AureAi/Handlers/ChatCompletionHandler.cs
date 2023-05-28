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

    public async Task GetChatCompletionsAsync(string text,
        Func<string> GetInstruction,
        Func<List<RoleDialogModel>> GetChatHistory,
        Func<string, Task> onChunkReceived,
        Func<Task> onChunkCompleted)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(text, GetInstruction, GetChatHistory);

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
        await onChunkCompleted();
    }

    private ChatCompletionsOptions PrepareOptions(string text,
        Func<string> GetInstruction,
        Func<List<RoleDialogModel>> GetChatHistory)
    {
        var prompt = File.ReadAllText(_settings.InstructionFile);
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, prompt)
            }
        };

        return chatCompletionsOptions;
    }
}
