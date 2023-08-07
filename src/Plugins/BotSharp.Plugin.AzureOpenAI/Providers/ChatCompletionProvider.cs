using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly AzureOpenAiSettings _settings;
    private readonly ILogger _logger;

    public ChatCompletionProvider(AzureOpenAiSettings settings, ILogger<ChatCompletionProvider> logger)
    {
        _settings = settings;
        _logger = logger;
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

        _logger.LogInformation(output);

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

    public List<FunctionDef> GetFunctions(string functionsJson)
    {
        var functions = new List<FunctionDef>();
        if (!string.IsNullOrEmpty(functionsJson))
        {
            functions = JsonSerializer.Deserialize<List<FunctionDef>>(functionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
        }

        return functions;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var client = new OpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = await client.GetChatCompletionsAsync(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
        {
            if (message.FunctionCall == null || message.FunctionCall.Arguments == null)
            {
                return false;
            }
            Console.Write(message.FunctionCall.Name);
            Console.Write(message.FunctionCall.Arguments);
            var funcContextIn = new RoleDialogModel(ChatRole.Function.ToString(), message.FunctionCall.Arguments)
            {
                FunctionName = message.FunctionCall.Name
            };

            // Execute functions
            await onMessageReceived(funcContextIn);

            // After function is executed, pass the result to LLM
            var fnResult = JsonSerializer.Deserialize<FunctionExecutionResult<object>>(funcContextIn.ExecutionResult);
            var fnJsonResult = JsonSerializer.Serialize(fnResult.Result);
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Function, fnJsonResult)
            {
                Name = funcContextIn.FunctionName
            });
            response = client.GetChatCompletions(_settings.DeploymentModel.ChatCompletionModel, chatCompletionsOptions);
        }

        choice = response.Value.Choices[0];
        message = choice.Message;

        _logger.LogInformation(message.Content);

        await onMessageReceived(new RoleDialogModel(ChatRole.Assistant.ToString(), message.Content));

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
                var funcContext = JsonSerializer.Deserialize<FunctionExecutionResult<object>>(message.Content);
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content)
                {
                    Name = funcContext.Name
                });
            }
            else
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
            }
        }

        return chatCompletionsOptions;
    }
}
