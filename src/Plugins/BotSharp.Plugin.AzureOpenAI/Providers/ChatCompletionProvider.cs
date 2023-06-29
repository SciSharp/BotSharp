using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly AzureOpenAiSettings _settings;

    public ChatCompletionProvider(AzureOpenAiSettings settings)
    {
        _settings = settings;
    }

    /*public async Task GetChatCompletionsAsync(List<RoleDialogModel> conversations,
        Func<string, Task> onChunkReceived)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(conversations);

        var response = await client.GetChatCompletionsStreamingAsync(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);
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
    }*/

    public List<RoleDialogModel> GetChatSamples(string sampleText)
    {
        var samples = new List<RoleDialogModel>();
        if (!string.IsNullOrEmpty(sampleText))
        {
            var lines = sampleText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var role = line.Substring(0, line.IndexOf(' ') - 1);
                var content = line.Substring(line.IndexOf(' ') + 1);

                samples.Add(new RoleDialogModel
                {
                    Role = role,
                    Text = content
                });
            }
        }
        return samples;
    }


    public async Task<string> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = await client.GetChatCompletionsStreamingAsync(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);
        using StreamingChatCompletions streaming = response.Value;

        string output = "";
        await foreach (var choice in streaming.GetChoicesStreaming())
        {
            await foreach (var message in choice.GetMessageStreaming())
            {
                if (message.Content == null)
                    continue;
                Console.Write(message.Content);
                output += message.Content;
            }
        }

        return output.Trim();
    }

    private ChatCompletionsOptions PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions();

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, agent.Instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, agent.Knowledges));
        }
        
        foreach (var message in GetChatSamples(agent.Samples))
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Text));
        }

        foreach (var message in conversations)
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Text));
        }

        return chatCompletionsOptions;
    }
}
