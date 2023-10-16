using Azure;
using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly AzureOpenAiSettings _settings;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    
    private string _model;

    public string Provider => "azure-openai";

    public ChatCompletionProvider(AzureOpenAiSettings settings, 
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook => 
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var (client, deploymentModel) = ProviderHelper.GetClient(_model, _settings);
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = client.GetChatCompletions(deploymentModel, chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
        {
            CurrentAgentId = agent.Id
        };

        if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
        {
            _logger.LogInformation($"[{agent.Name}]: {message.FunctionCall.Name}({message.FunctionCall.Arguments})");

            msg = new RoleDialogModel(AgentRole.Function, message.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = message.FunctionCall.Name,
                FunctionArgs = message.FunctionCall.Arguments
            };

            // Somethings LLM will generate a function name with agent name.
            if (!string.IsNullOrEmpty(msg.FunctionName))
            {
                msg.FunctionName = msg.FunctionName.Split('.').Last();
            }
        }

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model,
                PromptCount = response.Value.Usage.PromptTokens,
                CompletionCount = response.Value.Usage.CompletionTokens
            })).ToArray());

        return msg;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var (client, deploymentModel) = ProviderHelper.GetClient(_model, _settings);
        var chatCompletionsOptions = PrepareOptions(agent, conversations);

        var response = await client.GetChatCompletionsAsync(deploymentModel, chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model,
                PromptCount = response.Value.Usage.PromptTokens,
                CompletionCount = response.Value.Usage.CompletionTokens
            })).ToArray());

        if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
        {
            _logger.LogInformation($"[{agent.Name}]: {message.FunctionCall.Name}({message.FunctionCall.Arguments})");

            var funcContextIn = new RoleDialogModel(AgentRole.Function, message.Content)
            {
                CurrentAgentId = agent.Id,
                FunctionName = message.FunctionCall.Name,
                FunctionArgs = message.FunctionCall.Arguments
            };

            // Somethings LLM will generate a function name with agent name.
            if (!string.IsNullOrEmpty(funcContextIn.FunctionName))
            {
                funcContextIn.FunctionName = funcContextIn.FunctionName.Split('.').Last();
            }

            // Execute functions
            await onFunctionExecuting(funcContextIn);
        }
        else
        {
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


    protected ChatCompletionsOptions PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
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

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var message in samples)
        {
            chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
        }

        foreach (var function in agent.Functions)
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
        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.5"));
        var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.5"));
        chatCompletionsOptions.Temperature = temperature;
        chatCompletionsOptions.NucleusSamplingFactor = samplingFactor;
        // chatCompletionsOptions.FrequencyPenalty = 0;
        // chatCompletionsOptions.PresencePenalty = 0;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation("VERBOSE COMPLETION MESSAGES");
            var verbose = string.Join("\r\n", chatCompletionsOptions.Messages.Select(x =>
            {
                return x.Role == ChatRole.Function ?
                    $"{x.Role}: {x.Name} => {x.Content}" :
                    $"{x.Role}: {x.Content}";
            }));

            _logger.LogInformation(verbose);

            _logger.LogInformation("VERBOSE FUNCTIONS");
            verbose = string.Join("\r\n", chatCompletionsOptions.Functions.Select(x =>
            {
                return $"{x.Name}: {x.Description}\r\n{x.Parameters}";
            }));

            _logger.LogInformation(verbose);
        }

        return chatCompletionsOptions;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
