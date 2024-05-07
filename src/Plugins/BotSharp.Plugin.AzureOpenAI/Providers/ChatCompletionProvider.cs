using Azure.AI.OpenAI;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Loggers;
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
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;

    protected string _model;

    public virtual string Provider => "azure-openai";

    public ChatCompletionProvider(AzureOpenAiSettings settings, 
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, chatCompletionsOptions) = PrepareOptions(agent, conversations);
        chatCompletionsOptions.DeploymentName = _model;
        var response = client.GetChatCompletions(chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        RoleDialogModel responseMessage;

        if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
        {
            responseMessage = new RoleDialogModel(AgentRole.Function, message.Content)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                FunctionName = message.FunctionCall.Name,
                FunctionArgs = message.FunctionCall.Arguments
            };

            // Somethings LLM will generate a function name with agent name.
            if (!string.IsNullOrEmpty(responseMessage.FunctionName))
            {
                responseMessage.FunctionName = responseMessage.FunctionName.Split('.').Last();
            }
        }
        else if (choice.FinishReason == CompletionsFinishReason.ToolCalls)
        {
            // Add the assistant message with tool calls to the conversation history
            // ChatRequestAssistantMessage toolCallHistoryMessage = new(message);
            // chatCompletionsOptions.Messages.Add(toolCallHistoryMessage);

            // Add a new tool message for each tool call that is resolved
            var toolCall = message.ToolCalls.First() as ChatCompletionsFunctionToolCall;
            // var toolCallResponseMessage = GetToolCallResponseMessage(toolCall);
            // Now make a new request with all the messages thus far, including the original

            responseMessage = new RoleDialogModel(AgentRole.Function, message.Content)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                FunctionName = toolCall.Name,
                FunctionArgs = toolCall.Arguments
            };
        }
        else
        {
            responseMessage = new RoleDialogModel(AgentRole.Assistant, message.Content)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId
            };
        }

        // After chat completion hook
        foreach(var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Value.Usage.PromptTokens,
                CompletionCount = response.Value.Usage.CompletionTokens
            });
        }

        return responseMessage;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, 
        List<RoleDialogModel> conversations, 
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, chatCompletionsOptions) = PrepareOptions(agent, conversations);

        chatCompletionsOptions.DeploymentName = _model;
        var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
        var choice = response.Value.Choices[0];
        var message = choice.Message;

        var msg = new RoleDialogModel(AgentRole.Assistant, message.Content)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Value.Usage.PromptTokens,
                CompletionCount = response.Value.Usage.CompletionTokens
            });
        }

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
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, chatCompletionsOptions) = PrepareOptions(agent, conversations);
        chatCompletionsOptions.DeploymentName = _model;
        var response = await client.GetChatCompletionsStreamingAsync(chatCompletionsOptions);

        string output = "";
        await foreach (var choice in response)
        {
            if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
            {
                Console.Write(choice.FunctionArgumentsUpdate);
                    
                await onMessageReceived(new RoleDialogModel(ChatRole.Assistant.ToString(), choice.FunctionArgumentsUpdate));
                continue;
            }

            if (choice.ContentUpdate == null)
                continue;
            Console.Write(choice.ContentUpdate);

            _logger.LogInformation(choice.ContentUpdate);

            await onMessageReceived(new RoleDialogModel(choice.Role.ToString(), choice.ContentUpdate));
            
            output = "";
        }

        return true;
    }


    protected (string, ChatCompletionsOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();

        var chatCompletionsOptions = new ChatCompletionsOptions();
        
        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instruction = agentService.RenderedInstruction(agent);
            chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(agent.Knowledges));
        }

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var message in samples)
        {
            chatCompletionsOptions.Messages.Add(message.Role == AgentRole.User ? 
                new ChatRequestUserMessage(message.Content) :
                new ChatRequestAssistantMessage(message.Content));
        }

        foreach (var function in agent.Functions)
        {
            if (agentService.RenderFunction(agent, function))
            {
                var property = agentService.RenderFunctionProperty(agent, function);

                // legacy function call
                /*chatCompletionsOptions.Functions.Add(new FunctionDefinition
                {
                    Name = function.Name,
                    Description = function.Description,
                    Parameters = BinaryData.FromObjectAsJson(property)
                });*/

                // new chat tool
                chatCompletionsOptions.Tools.Add(new ChatCompletionsFunctionToolDefinition
                {
                    Name = function.Name,
                    Description = function.Description,
                    Parameters = BinaryData.FromObjectAsJson(property)
                });
            }
        }

        foreach (var message in conversations)
        {
            if (message.Role == ChatRole.Function)
            {
                chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(string.Empty)
                {
                    FunctionCall = new FunctionCall(message.FunctionName, message.FunctionArgs ?? String.Empty),
                });

                chatCompletionsOptions.Messages.Add(new ChatRequestFunctionMessage(message.FunctionName, message.Content));
                // chatCompletionsOptions.Messages.Add(new ChatRequestToolMessage(message.Content, message.ToolCallId));
            }
            else if (message.Role == ChatRole.User)
            {
                var userMessage = new ChatRequestUserMessage(message.Payload ?? message.Content)
                {
                    // To display Planner name in log
                    Name = message.FunctionName,
                };

                if (!string.IsNullOrEmpty(message.ImageUrl))
                {
                    var uri = new Uri(message.ImageUrl);
                    userMessage.MultimodalContentItems.Add(
                        new ChatMessageImageContentItem(uri, ChatMessageImageDetailLevel.Low));
                }

                chatCompletionsOptions.Messages.Add(userMessage);
            }
            else if (message.Role == ChatRole.Assistant)
            {
                chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(message.Content));
            }
        }

        // https://community.openai.com/t/cheat-sheet-mastering-temperature-and-top-p-in-chatgpt-api-a-few-tips-and-tricks-on-controlling-the-creativity-deterministic-output-of-prompt-responses/172683
        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var samplingFactor = float.Parse(state.GetState("sampling_factor", "0.0"));
        chatCompletionsOptions.Temperature = temperature;
        chatCompletionsOptions.NucleusSamplingFactor = samplingFactor;
        chatCompletionsOptions.MaxTokens = int.Parse(state.GetState("max_tokens", "1024"));
        // chatCompletionsOptions.FrequencyPenalty = 0;
        // chatCompletionsOptions.PresencePenalty = 0;

        var prompt = GetPrompt(chatCompletionsOptions);

        return (prompt, chatCompletionsOptions);
    }

    private string GetPrompt(ChatCompletionsOptions chatCompletionsOptions)
    {
        var prompt = string.Empty;

        if (chatCompletionsOptions.Messages.Count > 0)
        {
            // System instruction
            var verbose = string.Join("\r\n", chatCompletionsOptions.Messages
                .Where(x => x.Role == AgentRole.System)
                .Select(x => x as ChatRequestSystemMessage).Select(x =>
                {
                    if (!string.IsNullOrEmpty(x.Name))
                    {
                        // To display Agent name in log
                        return $"[{x.Name}]: {x.Content}";
                    }
                    return $"{x.Role}: {x.Content}";
                }));
            prompt += $"{verbose}\r\n";

            prompt += "\r\n[CONVERSATION]";
            verbose = string.Join("\r\n", chatCompletionsOptions.Messages
                .Where(x => x.Role != AgentRole.System).Select(x =>
                {
                    if (x.Role == ChatRole.Function)
                    {
                        var m = x as ChatRequestFunctionMessage;
                        return $"{m.Role}: {m.Content}";
                    }
                    else if (x.Role == ChatRole.User)
                    {
                        var m = x as ChatRequestUserMessage;
                        return !string.IsNullOrEmpty(m.Name) && m.Name != "route_to_agent" ?
                            $"{m.Name}: {m.Content}" :
                            $"{m.Role}: {m.Content}";
                    }
                    else if (x.Role == ChatRole.Assistant)
                    {
                        var m = x as ChatRequestAssistantMessage;
                        return m.FunctionCall != null ?
                            $"{m.Role}: Call function {m.FunctionCall.Name}({m.FunctionCall.Arguments})" :
                            $"{m.Role}: {m.Content}";
                    }
                    else
                    {
                        throw new NotImplementedException("Not found role");
                    }
                }));
            prompt += $"\r\n{verbose}\r\n";
        }

        if (chatCompletionsOptions.Tools.Count > 0)
        {
            var functions = string.Join("\r\n", chatCompletionsOptions.Tools.Select(x =>
            {
                var fn = x as ChatCompletionsFunctionToolDefinition;
                return $"\r\n{fn.Name}: {fn.Description}\r\n{fn.Parameters}";
            }));
            prompt += $"\r\n[FUNCTIONS]{functions}\r\n";
        }

        return prompt;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    ChatRequestToolMessage GetToolCallResponseMessage(ChatCompletionsToolCall toolCall)
    {
        var functionToolCall = toolCall as ChatCompletionsFunctionToolCall;
        // Validate and process the JSON arguments for the function call
        string unvalidatedArguments = functionToolCall.Arguments;
        var functionResultData = (object)null; // GetYourFunctionResultData(unvalidatedArguments);
                                               // Here, replacing with an example as if returned from "GetYourFunctionResultData"
        functionResultData = "31 celsius";
        return new ChatRequestToolMessage(functionResultData.ToString(), toolCall.Id);
    }
}
