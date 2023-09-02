using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class GPT4CompletionProvider : IChatCompletion
{
    private readonly AzureOpenAiSettings _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public GPT4CompletionProvider(AzureOpenAiSettings settings, 
        ILogger<GPT4CompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    private OpenAIClient GetClient()
    {
        var client = new OpenAIClient(new Uri(_settings.GPT4.Endpoint), new AzureKeyCredential(_settings.GPT4.ApiKey));
        return client;
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

    public List<FunctionDef> GetFunctions(List<string> functionsJson)
    {
        var functions = functionsJson?.Select(x => JsonSerializer.Deserialize<FunctionDef>(x, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        }))?.ToList() ?? new List<FunctionDef>();

        return functions;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var client = GetClient();
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = await client.GetChatCompletionsAsync(_settings.GPT4.DeploymentModel, chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
        {
            _logger.LogInformation($"[{agent.Name}]: {message.FunctionCall.Name} => {message.FunctionCall.Arguments}");

            var funcContextIn = new RoleDialogModel(AgentRole.Function, message.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = message.FunctionCall.Name,
                FunctionArgs = message.FunctionCall.Arguments,
                Channel = conversations.Last().Channel
            };

            // Execute functions
            await onFunctionExecuting(funcContextIn);
        }
        else
        {
            _logger.LogInformation($"[{agent.Name}] {message.Role}: {message.Content}");

            var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
            {
                CurrentAgentId= agent.Id,
                Channel = conversations.Last().Channel
            };

            // Text response received
            await onMessageReceived(msg);
        }

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = await client.GetChatCompletionsStreamingAsync(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);
        using StreamingChatCompletions streaming = response.Value;

        string output = "";
        await foreach (var choice in streaming.GetChoicesStreaming())
        {
            if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
            {
                var args = "";
                await foreach (var message in choice.GetMessageStreaming())
                {
                    if (message.FunctionCall == null || message.FunctionCall.Arguments == null)
                        continue;
                    Console.Write(message.FunctionCall.Arguments);
                    args += message.FunctionCall.Arguments;
                    
                }
                await onMessageReceived(new RoleDialogModel(ChatRole.Assistant.ToString(), args));
                continue;
            }

            await foreach (var message in choice.GetMessageStreaming())
            {
                if (message.Content == null)
                    continue;
                Console.Write(message.Content);
                output += message.Content;

                _logger.LogInformation(message.Content);

                await onMessageReceived(new RoleDialogModel(message.Role.ToString(), message.Content));
            }
            
            output = "";
        }

        return true;
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

        var functions = GetFunctions(agent.Functions);
        foreach (var function in functions)
        {
            chatCompletionsOptions.Functions.Add(new FunctionDefinition
            {
                Name = function.Name,
                Description = function.Description,
                Parameters = BinaryData.FromObjectAsJson(function.Parameters)
            });
        }

        foreach (var message in conversations)
        {
            if (message.Role == ChatRole.Function)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content)
                {
                    Name = message.FunctionName
                });
            }
            else
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
            }
        }

        // https://community.openai.com/t/cheat-sheet-mastering-temperature-and-top-p-in-chatgpt-api-a-few-tips-and-tricks-on-controlling-the-creativity-deterministic-output-of-prompt-responses/172683
        chatCompletionsOptions.Temperature = 0.5f;
        chatCompletionsOptions.NucleusSamplingFactor = 0.5f;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            var verbose = string.Join("\n", chatCompletionsOptions.Messages.Select(x =>
            {
                return x.Role == ChatRole.Function ?
                    $"{x.Role}: {x.Name} {x.Content}" :
                    $"{x.Role}: {x.Content}";
            }));
            _logger.LogInformation(verbose);
        }

        return chatCompletionsOptions;
    }
}
