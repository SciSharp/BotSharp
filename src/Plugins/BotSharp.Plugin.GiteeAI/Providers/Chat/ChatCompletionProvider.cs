using BotSharp.Abstraction.Agents.Constants;
using BotSharp.Abstraction.Diagnostics;
using BotSharp.Abstraction.Files;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Diagnostics;
using static BotSharp.Abstraction.Diagnostics.ModelDiagnostics;

namespace BotSharp.Plugin.GiteeAI.Providers.Chat;

/// <summary>
/// 模力方舟的文本对话
/// </summary>
public class ChatCompletionProvider(
    ILogger<ChatCompletionProvider> logger,
    IServiceProvider services) : IChatCompletion
{
    protected string _model = string.Empty;

    public virtual string Provider => "gitee-ai";

    public string Model => _model;

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = services.GetServices<IContentGeneratingHook>().ToList();
        var convService = services.GetService<IConversationStateService>();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        using (var activity = ModelDiagnostics.StartCompletionActivity(null, _model, Provider, prompt, convService))
        {
            var response = chatClient.CompleteChat(messages, options);
            var value = response.Value;
            var reason = value.FinishReason;
            var content = value.Content;
            var text = content.FirstOrDefault()?.Text ?? string.Empty;

            activity?.SetTag(ModelDiagnosticsTags.FinishReason, reason);

            RoleDialogModel responseMessage;
            if (reason == ChatFinishReason.FunctionCall || reason == ChatFinishReason.ToolCalls)
            {
                var toolCall = value.ToolCalls.FirstOrDefault();
                responseMessage = new RoleDialogModel(AgentRole.Function, text)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                    ToolCallId = toolCall?.Id,
                    FunctionName = toolCall?.FunctionName,
                    FunctionArgs = toolCall?.FunctionArguments?.ToString()
                };

                // Somethings LLM will generate a function name with agent name.
                if (!string.IsNullOrEmpty(responseMessage.FunctionName))
                {
                    responseMessage.FunctionName = responseMessage.FunctionName.Split('.').Last();
                }
            }
            else
            {
                responseMessage = new RoleDialogModel(AgentRole.Assistant, text)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                };
            }

            var tokenUsage = response?.Value?.Usage;
            var inputTokenDetails = response?.Value?.Usage?.InputTokenDetails;

            activity?.SetTag(ModelDiagnosticsTags.InputTokens, (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0));
            activity?.SetTag(ModelDiagnosticsTags.OutputTokens, tokenUsage?.OutputTokenCount ?? 0);
            activity?.SetTag(ModelDiagnosticsTags.OutputTokens, tokenUsage?.OutputTokenCount ?? 0);

            // After chat completion hook
            foreach (var hook in contentHooks)
            {
                await hook.AfterGenerated(responseMessage, new TokenStatsModel
                {
                    Prompt = prompt,
                    Provider = Provider,
                    Model = _model,
                    TextInputTokens = response.Value?.Usage?.InputTokenCount ?? 0,
                    TextOutputTokens = response.Value?.Usage?.OutputTokenCount ?? 0
                });
            }
            activity?.SetTag("output", responseMessage.Content);
            return responseMessage;
        }
    }

    public async Task<RoleDialogModel> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onStreamResponseReceived)
    {
        var contentHooks = services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        StringBuilder? contentBuilder = null;
        Dictionary<int, string>? toolCallIdsByIndex = null;
        Dictionary<int, string>? functionNamesByIndex = null;
        Dictionary<int, StringBuilder>? functionArgumentBuildersByIndex = null;

        var client = ProviderHelper.GetClient(Provider, _model, services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = chatClient.CompleteChatStreamingAsync(messages, options);

        await foreach (var choice in response)
        {
            TrackStreamingToolingUpdate(choice.ToolCallUpdates, ref toolCallIdsByIndex, ref functionNamesByIndex, ref functionArgumentBuildersByIndex);

            if (!choice.ContentUpdate.IsNullOrEmpty() && choice.ContentUpdate[0] != null)
            {
                foreach (var contentPart in choice.ContentUpdate)
                {
                    if (contentPart.Kind == ChatMessageContentPartKind.Text)
                    {
                        (contentBuilder ??= new()).Append(contentPart.Text);
                    }
                }

                logger.LogInformation(choice.ContentUpdate[0]?.Text);

                if (!string.IsNullOrEmpty(choice.ContentUpdate[0]?.Text))
                {
                    var msg = new RoleDialogModel(choice.Role?.ToString() ?? ChatMessageRole.Assistant.ToString(), choice.ContentUpdate[0]?.Text ?? string.Empty);

                    await onStreamResponseReceived(msg);
                }
            }
        }

        // Get any response content that was streamed.
        string content = contentBuilder?.ToString() ?? string.Empty;

        RoleDialogModel responseMessage = new(ChatMessageRole.Assistant.ToString(), content);

        var tools = ConvertToolCallUpdatesToFunctionToolCalls(ref toolCallIdsByIndex, ref functionNamesByIndex, ref functionArgumentBuildersByIndex);

        foreach (var tool in tools)
        {
            tool.CurrentAgentId = agent.Id;
            tool.MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;
            await onStreamResponseReceived(tool);
        }

        if (tools.Length > 0)
        {
            responseMessage = tools[0];
        }

        return responseMessage;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = await chatClient.CompleteChatAsync(messages, options);
        var value = response.Value;
        var reason = value.FinishReason;
        var content = value.Content;
        var text = content.FirstOrDefault()?.Text ?? string.Empty;

        var msg = new RoleDialogModel(AgentRole.Assistant, text)
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
                TextInputTokens = response.Value?.Usage?.InputTokenCount ?? 0,
                TextOutputTokens = response.Value?.Usage?.OutputTokenCount ?? 0
            });
        }

        if (reason == ChatFinishReason.FunctionCall || reason == ChatFinishReason.ToolCalls)
        {
            var toolCall = value.ToolCalls?.FirstOrDefault();
            logger.LogInformation($"[{agent.Name}]: {toolCall?.FunctionName}({toolCall?.FunctionArguments})");

            var funcContextIn = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = toolCall?.Id,
                FunctionName = toolCall?.FunctionName,
                FunctionArgs = toolCall?.FunctionArguments?.ToString()
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
        var client = ProviderHelper.GetClient(Provider, _model, services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = chatClient.CompleteChatStreamingAsync(messages, options);

        await foreach (var choice in response)
        {
            if (choice.FinishReason == ChatFinishReason.FunctionCall || choice.FinishReason == ChatFinishReason.ToolCalls)
            {
                var update = choice.ToolCallUpdates?.FirstOrDefault()?.FunctionArgumentsUpdate?.ToString() ?? string.Empty;
                logger.LogInformation(update);

                await onMessageReceived(new RoleDialogModel(AgentRole.Assistant, update));
                continue;
            }

            if (choice.ContentUpdate.IsNullOrEmpty()) continue;

            logger.LogInformation(choice.ContentUpdate[0]?.Text);

            await onMessageReceived(new RoleDialogModel(choice.Role?.ToString() ?? ChatMessageRole.Assistant.ToString(), choice.ContentUpdate[0]?.Text ?? string.Empty));
        }

        return true;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = services.GetRequiredService<IAgentService>();
        var state = services.GetRequiredService<IConversationStateService>();
        var fileStorage = services.GetRequiredService<IFileStorageService>();
        var settingsService = services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;

        var messages = new List<ChatMessage>();
        float? temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
                        ? tokens
                        : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;


        state.SetState("temperature", temperature.ToString());
        state.SetState("max_tokens", maxTokens.ToString());

        var options = new ChatCompletionOptions()
        {
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens
        };

        var functions = agent.Functions.Concat(agent.SecondaryFunctions ?? []);
        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function)) continue;

            var property = agentService.RenderFunctionProperty(agent, function);

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: function.Name,
                functionDescription: function.Description,
                functionParameters: BinaryData.FromObjectAsJson(property)));
        }

        if (!string.IsNullOrEmpty(agent.Instruction) || !agent.SecondaryInstructions.IsNullOrEmpty())
        {
            var text = agentService.RenderInstruction(agent);
            messages.Add(new SystemChatMessage(text));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(new SystemChatMessage(agent.Knowledges));
        }

        var filteredMessages = conversations.Select(x => x).ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                messages.Add(new AssistantChatMessage(new List<ChatToolCall>
                {
                    ChatToolCall.CreateFunctionToolCall(message.FunctionName, message.FunctionName, BinaryData.FromString(message.FunctionArgs ?? string.Empty))
                }));

                messages.Add(new ToolChatMessage(message.FunctionName, message.Content));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                messages.Add(new UserChatMessage(text));
            }
            else if (message.Role == AgentRole.Assistant)
            {
                messages.Add(new AssistantChatMessage(message.Content));
            }
        }

        var prompt = GetPrompt(messages, options);
        return (prompt, messages, options);
    }

    private string GetPrompt(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
    {
        var prompt = string.Empty;

        if (!messages.IsNullOrEmpty())
        {
            // System instruction
            var verbose = string.Join("\r\n", messages
                .Select(x => x as SystemChatMessage)
                .Where(x => x != null)
                .Select(x =>
                {
                    if (!string.IsNullOrEmpty(x.ParticipantName))
                    {
                        // To display Agent name in log
                        return $"[{x.ParticipantName}]: {x.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }
                    return $"{AgentRole.System}: {x.Content.FirstOrDefault()?.Text ?? string.Empty}";
                }));
            prompt += $"{verbose}\r\n";

            prompt += "\r\n[CONVERSATION]";
            verbose = string.Join("\r\n", messages
                .Where(x => x as SystemChatMessage == null)
                .Select(x =>
                {
                    var fnMessage = x as ToolChatMessage;
                    if (fnMessage != null)
                    {
                        return $"{AgentRole.Function}: {fnMessage.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }

                    var userMessage = x as UserChatMessage;
                    if (userMessage != null)
                    {
                        var content = x.Content.FirstOrDefault()?.Text ?? string.Empty;
                        return !string.IsNullOrEmpty(userMessage.ParticipantName) && userMessage.ParticipantName != "route_to_agent" ?
                            $"{userMessage.ParticipantName}: {content}" :
                            $"{AgentRole.User}: {content}";
                    }

                    var assistMessage = x as AssistantChatMessage;
                    if (assistMessage != null)
                    {
                        var toolCall = assistMessage.ToolCalls?.FirstOrDefault();
                        return toolCall != null ?
                            $"{AgentRole.Assistant}: Call function {toolCall?.FunctionName}({toolCall?.FunctionArguments})" :
                            $"{AgentRole.Assistant}: {assistMessage.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }

                    return string.Empty;
                }));
            prompt += $"\r\n{verbose}\r\n";
        }

        if (!options.Tools.IsNullOrEmpty())
        {
            var functions = string.Join("\r\n", options.Tools.Select(fn =>
            {
                return $"\r\n{fn.FunctionName}: {fn.FunctionDescription}\r\n{fn.FunctionParameters}";
            }));
            prompt += $"\r\n[FUNCTIONS]{functions}\r\n";
        }

        return prompt;
    }

    private static void TrackStreamingToolingUpdate(
        IReadOnlyList<StreamingChatToolCallUpdate>? updates,
        ref Dictionary<int, string>? toolCallIdsByIndex,
        ref Dictionary<int, string>? functionNamesByIndex,
        ref Dictionary<int, StringBuilder>? functionArgumentBuildersByIndex)
    {
        if (updates is null)
        {
            // Nothing to track.
            return;
        }

        foreach (var update in updates)
        {
            // If we have an ID, ensure the index is being tracked. Even if it's not a function update,
            // we want to keep track of it so we can send back an error.
            if (!string.IsNullOrWhiteSpace(update.ToolCallId))
            {
                (toolCallIdsByIndex ??= [])[update.Index] = update.ToolCallId;
            }

            // Ensure we're tracking the function's name.
            if (!string.IsNullOrWhiteSpace(update.FunctionName))
            {
                (functionNamesByIndex ??= [])[update.Index] = update.FunctionName;
            }

            // Ensure we're tracking the function's arguments.
            if (update.FunctionArgumentsUpdate is not null && !update.FunctionArgumentsUpdate.ToMemory().IsEmpty)
            {
                if (!(functionArgumentBuildersByIndex ??= []).TryGetValue(update.Index, out StringBuilder? arguments))
                {
                    functionArgumentBuildersByIndex[update.Index] = arguments = new();
                }

                arguments.Append(update.FunctionArgumentsUpdate.ToString());
            }
        }
    }

    private static RoleDialogModel[] ConvertToolCallUpdatesToFunctionToolCalls(
        ref Dictionary<int, string>? toolCallIdsByIndex,
        ref Dictionary<int, string>? functionNamesByIndex,
        ref Dictionary<int, StringBuilder>? functionArgumentBuildersByIndex)
    {
        RoleDialogModel[] toolCalls = [];
        if (toolCallIdsByIndex is { Count: > 0 })
        {
            toolCalls = new RoleDialogModel[toolCallIdsByIndex.Count];

            int i = 0;
            foreach (KeyValuePair<int, string> toolCallIndexAndId in toolCallIdsByIndex)
            {
                string? functionName = null;
                StringBuilder? functionArguments = null;

                functionNamesByIndex?.TryGetValue(toolCallIndexAndId.Key, out functionName);
                functionArgumentBuildersByIndex?.TryGetValue(toolCallIndexAndId.Key, out functionArguments);

                toolCalls[i] = new RoleDialogModel(AgentRole.Function, string.Empty)
                {
                    FunctionName = functionName ?? string.Empty,
                    FunctionArgs = functionArguments?.ToString() ?? string.Empty,
                };
                i++;
            }

            Debug.Assert(i == toolCalls.Length);
        }

        return toolCalls;
    }

}
