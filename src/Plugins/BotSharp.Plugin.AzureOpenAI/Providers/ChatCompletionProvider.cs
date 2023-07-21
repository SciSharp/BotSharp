using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly AzureOpenAiSettings _settings;

    public ChatCompletionProvider(AzureOpenAiSettings settings)
    {
        _settings = settings;
    }

    public string GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = client.GetChatCompletions(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);

        string output = "";
        foreach (var choice in response.Value.Choices)
        {
            var message = choice.Message;
            if (message.Content == null)
                continue;
            Console.Write(message.Content);
            output += message.Content;
        }

        return output.Trim();
    }

    public List<RoleDialogModel> GetChatSamples(string sampleText)
    {
        var samples = new List<RoleDialogModel>();
        if (string.IsNullOrEmpty(sampleText))
        {
            return samples;
        }

        var lines = sampleText.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line.Trim()))
            {
                continue;
            }
            var role = line.Substring(0, line.IndexOf(' ') - 1).Trim();
            var content = line.Substring(line.IndexOf(' ') + 1).Trim();

            // comments
            if (role == "##")
            {
                continue;
            }

            samples.Add(new RoleDialogModel(role, content));
        }

        return samples;
    }


    public async Task<string> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
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

        var samples = GetChatSamples(agent.Samples);
        foreach (var message in samples)
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
