#pragma warning disable OPENAI001
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Core.Infrastructures.Streams;
using BotSharp.Core.MessageHub;
using OpenAI.Chat;
using System.Net.Http;

namespace BotSharp.Plugin.OpenAI.Providers.Chat;

public partial class ChatCompletionProvider
{
    private async Task<RoleDialogModel> InnerGetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, apiKey: _apiKey, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = chatClient.CompleteChat(messages, options);
        var value = response.Value;
        var reason = value.FinishReason;
        var content = value.Content;
        var text = content.FirstOrDefault()?.Text ?? string.Empty;

        RoleDialogModel responseMessage;
        if (reason == ChatFinishReason.FunctionCall || reason == ChatFinishReason.ToolCalls)
        {
            _logger.LogInformation($"Action: {nameof(InnerGetChatCompletions)}, Reason: {reason}, Agent: {agent.Name}, ToolCalls: {string.Join(",", value.ToolCalls.Select(x => x.FunctionName))}");

            var toolCall = value.ToolCalls.FirstOrDefault();
            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = toolCall?.Id,
                FunctionName = toolCall?.FunctionName,
                FunctionArgs = toolCall?.FunctionArguments?.ToString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };

            // Somethings LLM will generate a function name with agent name.
            responseMessage.FunctionName = responseMessage.FunctionName.NormalizeFunctionName();
        }
        else if (reason == ChatFinishReason.Length)
        {
            _logger.LogWarning($"Action: {nameof(InnerGetChatCompletions)}, Reason: {reason}, Agent: {agent.Name}, MaxOutputTokens: {options.MaxOutputTokenCount}, Content:{text}");

            responseMessage = new RoleDialogModel(AgentRole.Assistant, $"AI response exceeded max output length")
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                StopCompletion = true
            };
        }
        else
        {
            responseMessage = new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                RenderedInstruction = string.Join("\r\n", renderedInstructions),
                Annotations = value.Annotations?.Select(x => new ChatAnnotation
                {
                    Title = x.WebResourceTitle,
                    Url = x.WebResourceUri.AbsoluteUri,
                    StartIndex = x.StartIndex,
                    EndIndex = x.EndIndex
                })?.ToList()
            };
        }

        var tokenUsage = response.Value?.Usage;
        var inputTokenDetails = response.Value?.Usage?.InputTokenDetails;

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0),
                CachedTextInputTokens = inputTokenDetails?.CachedTokenCount ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokenCount ?? 0
            });
        }

        return responseMessage;
    }


    private async Task<bool> InnerGetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, apiKey: _apiKey, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = await chatClient.CompleteChatAsync(messages, options);
        var value = response.Value;
        var reason = value.FinishReason;
        var content = value.Content;
        var text = content.FirstOrDefault()?.Text ?? string.Empty;

        var msg = new RoleDialogModel(AgentRole.Assistant, text)
        {
            CurrentAgentId = agent.Id,
            RenderedInstruction = string.Join("\r\n", renderedInstructions)
        };

        var tokenUsage = response?.Value?.Usage;
        var inputTokenDetails = response?.Value?.Usage?.InputTokenDetails;

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0),
                CachedTextInputTokens = inputTokenDetails?.CachedTokenCount ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokenCount ?? 0
            });
        }

        if (reason == ChatFinishReason.FunctionCall || reason == ChatFinishReason.ToolCalls)
        {
            var toolCall = value.ToolCalls?.FirstOrDefault();
            _logger.LogInformation($"[{agent.Name}]: {toolCall?.FunctionName}({toolCall?.FunctionArguments})");

            var funcContextIn = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = toolCall?.Id,
                FunctionName = toolCall?.FunctionName,
                FunctionArgs = toolCall?.FunctionArguments?.ToString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };

            // Somethings LLM will generate a function name with agent name.
            funcContextIn.FunctionName = funcContextIn.FunctionName.NormalizeFunctionName();

            // Execute functions
            await onFunctionExecuting(funcContextIn);
        }
        else if (reason == ChatFinishReason.Length)
        {
            _logger.LogWarning($"Action: {nameof(InnerGetChatCompletionsAsync)}, Reason: {reason}, Agent: {agent.Name}, MaxOutputTokens: {options.MaxOutputTokenCount}, Content:{text}");

            msg = new RoleDialogModel(AgentRole.Assistant, $"AI response exceeded max output length")
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                StopCompletion = true,
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
            await onMessageReceived(msg);
        }
        else
        {
            // Text response received
            msg = new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                RenderedInstruction = string.Join("\r\n", renderedInstructions),
                Annotations = value.Annotations?.Select(x => new ChatAnnotation
                {
                    Title = x.WebResourceTitle,
                    Url = x.WebResourceUri.AbsoluteUri,
                    StartIndex = x.StartIndex,
                    EndIndex = x.EndIndex
                })?.ToList()
            };
            await onMessageReceived(msg);
        }

        return true;
    }

    private async Task<RoleDialogModel> InnerGetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = ProviderHelper.GetClient(Provider, _model, apiKey: _apiKey, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var hub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        var conv = _services.GetRequiredService<IConversationService>();
        var messageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;

        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);
        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        hub.Push(new()
        {
            EventName = ChatEvent.BeforeReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = new RoleDialogModel(AgentRole.Assistant, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId
            }
        });

        using var textStream = new RealtimeTextStream();
        var toolCalls = new List<StreamingChatToolCallUpdate>();
        ChatTokenUsage? tokenUsage = null;

        var responseMessage = new RoleDialogModel(AgentRole.Assistant, string.Empty)
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId
        };

        var streamingCancellation = _services.GetRequiredService<IConversationCancellationService>();
        var cancellationToken = streamingCancellation.GetToken(conv.ConversationId);

        try
        {
            await foreach (var choice in chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken))
            {
                tokenUsage = choice.Usage;

                if (!choice.ToolCallUpdates.IsNullOrEmpty())
                {
                    toolCalls.AddRange(choice.ToolCallUpdates);
                }

                if (!choice.ContentUpdate.IsNullOrEmpty())
                {
                    var text = choice.ContentUpdate[0]?.Text ?? string.Empty;
                    textStream.Collect(text);
#if DEBUG
                    _logger.LogDebug($"Stream Content update: {text}");
#endif

                    hub.Push(new()
                    {
                        EventName = ChatEvent.OnReceiveLlmStreamMessage,
                        RefId = conv.ConversationId,
                        Data = new RoleDialogModel(AgentRole.Assistant, text)
                        {
                            CurrentAgentId = agent.Id,
                            MessageId = messageId
                        }
                    });
                }

                if (choice.FinishReason == ChatFinishReason.ToolCalls || choice.FinishReason == ChatFinishReason.FunctionCall)
                {
                    var meta = toolCalls.FirstOrDefault(x => !string.IsNullOrEmpty(x.FunctionName));
                    var functionName = meta?.FunctionName;
                    var toolCallId = meta?.ToolCallId;
                    var args = toolCalls.Where(x => x.FunctionArgumentsUpdate != null).Select(x => x.FunctionArgumentsUpdate.ToString()).ToList();
                    var functionArguments = string.Join(string.Empty, args);

#if DEBUG
                    _logger.LogDebug($"Tool Call (id: {toolCallId}) => {functionName}({functionArguments})");
#endif

                    responseMessage = new RoleDialogModel(AgentRole.Function, string.Empty)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        ToolCallId = toolCallId,
                        FunctionName = functionName,
                        FunctionArgs = functionArguments
                    };
                }
                else if (choice.FinishReason == ChatFinishReason.Stop)
                {
                    var allText = textStream.GetText();
#if DEBUG
                    _logger.LogDebug($"Stream text Content: {allText}");
#endif

                    responseMessage = new RoleDialogModel(AgentRole.Assistant, allText)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        IsStreaming = true
                    };
                }
                else if (choice.FinishReason.HasValue)
                {
                    var text = choice.FinishReason == ChatFinishReason.Length ? "Model reached the maximum number of tokens allowed."
                        : choice.FinishReason == ChatFinishReason.ContentFilter ? "Content is omitted due to content filter rule."
                        : choice.FinishReason.Value.ToString();
                    responseMessage = new RoleDialogModel(AgentRole.Assistant, text)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        IsStreaming = true
                    };
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming was cancelled for conversation {ConversationId}", conv.ConversationId);
        }

        // Build responseMessage from collected text when cancelled before FinishReason
        if (cancellationToken.IsCancellationRequested && string.IsNullOrEmpty(responseMessage.Content))
        {
            var allText = textStream.GetText();
            responseMessage = new RoleDialogModel(AgentRole.Assistant, allText)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId,
                IsStreaming = true
            };
        }

        hub.Push(new()
        {
            EventName = ChatEvent.AfterReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = responseMessage
        });


        var inputTokenDetails = tokenUsage?.InputTokenDetails;
        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0),
                CachedTextInputTokens = inputTokenDetails?.CachedTokenCount ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokenCount ?? 0
            });
        }

        return responseMessage;
    }



    #region Private methods
    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;
        renderedInstructions = [];

        var messages = new List<ChatMessage>();
        var options = InitChatCompletionOption(agent);

        // Prepare instruction and functions
        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            renderedInstructions.Add(instruction);
            messages.Add(new SystemChatMessage(instruction));
        }

        // Render functions
        if (options.WebSearchOptions == null)
        {
            foreach (var function in functions)
            {
                if (!agentService.RenderFunction(agent, function, renderData))
                {
                    continue;
                }

                var property = agentService.RenderFunctionProperty(agent, function, renderData);

                options.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: function.Name,
                    functionDescription: function.Description,
                    functionParameters: BinaryData.FromObjectAsJson(property)));
            }
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(new SystemChatMessage(agent.Knowledges));
        }

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var sample in samples)
        {
            messages.Add(sample.Role == AgentRole.User ? new UserChatMessage(sample.Content) : new AssistantChatMessage(sample.Content));
        }

        var filteredMessages = conversations.Select(x => x).ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        var imageDetailLevel = ChatImageDetailLevel.Auto;
        if (allowMultiModal)
        {
            imageDetailLevel = ParseChatImageDetailLevel(_state.GetState("chat_image_detail_level"));
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                messages.Add(new AssistantChatMessage(new List<ChatToolCall>
                {
                    ChatToolCall.CreateFunctionToolCall(message.ToolCallId.IfNullOrEmptyAs(message.FunctionName), message.FunctionName, BinaryData.FromString(message.FunctionArgs ?? "{}"))
                }));

                messages.Add(new ToolChatMessage(message.ToolCallId.IfNullOrEmptyAs(message.FunctionName), message.LlmContent));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = message.LlmContent;
                var textPart = ChatMessageContentPart.CreateTextPart(text);
                var contentParts = new List<ChatMessageContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectMessageContentParts(contentParts, message.Files, imageDetailLevel);
                }
                messages.Add(new UserChatMessage(contentParts) { ParticipantName = message.FunctionName });
            }
            else if (message.Role == AgentRole.Assistant)
            {
                var text = message.LlmContent;
                var textPart = ChatMessageContentPart.CreateTextPart(text);
                var contentParts = new List<ChatMessageContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectMessageContentParts(contentParts, message.Files, imageDetailLevel);
                }
                messages.Add(new AssistantChatMessage(contentParts));
            }
        }

        var prompt = GetPrompt(messages, options);
        return (prompt, messages, options);
    }

    private void CollectMessageContentParts(List<ChatMessageContentPart> contentParts, List<BotSharpFile> files, ChatImageDetailLevel imageDetailLevel)
    {
        foreach (var file in files)
        {
            if (!string.IsNullOrEmpty(file.FileData))
            {
                var (contentType, binary) = FileUtility.GetFileInfoFromData(file.FileData);
                contentType = contentType.IfNullOrEmptyAs(file.ContentType);
                var contentPart = IsImageContentType(contentType)
                    ? ChatMessageContentPart.CreateImagePart(binary, contentType, imageDetailLevel)
                    : ChatMessageContentPart.CreateFilePart(binary, contentType, file.FileFullName);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileStorageUrl))
            {
                var fileStorage = _services.GetRequiredService<IFileStorageService>();
                var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
                var contentType = FileUtility.GetFileContentType(file.FileStorageUrl).IfNullOrEmptyAs(file.ContentType);
                var contentPart = IsImageContentType(contentType)
                    ? ChatMessageContentPart.CreateImagePart(binary, contentType, imageDetailLevel)
                    : ChatMessageContentPart.CreateFilePart(binary, contentType, file.FileFullName);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileUrl))
            {
                var contentType = FileUtility.GetFileContentType(file.FileUrl).IfNullOrEmptyAs(file.ContentType);
                if (IsImageContentType(contentType))
                {
                    var uri = new Uri(file.FileUrl);
                    var contentPart = ChatMessageContentPart.CreateImagePart(uri, imageDetailLevel);
                    contentParts.Add(contentPart);
                }
                else
                {
                    try
                    {
                        var http = _services.GetRequiredService<IHttpClientFactory>();
                        using var client = http.CreateClient();
                        var bytes = client.GetByteArrayAsync(file.FileUrl).GetAwaiter().GetResult();
                        var binary = BinaryData.FromBytes(bytes);
                        var contentPart = ChatMessageContentPart.CreateFilePart(binary, contentType, file.FileFullName);
                        contentParts.Add(contentPart);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to download FileUrl for chat file part (url: {file.FileUrl}).");
                    }
                }
            }
        }
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

    private ChatCompletionOptions InitChatCompletionOption(Agent agent)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);

        // Reasoning
        float? temperature = float.Parse(_state.GetState("temperature", "0.0"));
        var (reasoningTemp, reasoningEffortLevel) = ParseReasoning(settings?.Reasoning, agent);
        if (reasoningTemp.HasValue)
        {
            temperature = reasoningTemp.Value;
        }

        // Web search
        ChatWebSearchOptions? webSearchOptions = null;
        if (settings?.WebSearch != null)
        {
            temperature = null;
            reasoningEffortLevel = null;
            webSearchOptions = new();
        }

        var maxTokens = int.TryParse(_state.GetState("max_tokens"), out var tokens)
                        ? tokens
                        : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;

        return new ChatCompletionOptions()
        {
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens,
            ReasoningEffortLevel = reasoningEffortLevel,
            WebSearchOptions = webSearchOptions
        };
    }

    /// <summary>
    /// Parse reasoning setting: returns (temperature, reasoning effort level)
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="agent"></param>
    /// <returns></returns>
    private (float?, ChatReasoningEffortLevel?) ParseReasoning(ReasoningSetting? settings, Agent agent)
    {
        float? temperature = null;
        ChatReasoningEffortLevel? reasoningEffortLevel = null;

        var level = _state.GetState("reasoning_effort_level");

        if (string.IsNullOrEmpty(level) && _model == agent?.LlmConfig?.Model)
        {
            level = agent?.LlmConfig?.ReasoningEffortLevel;
        }

        if (settings == null)
        {
            reasoningEffortLevel = ParseReasoningEffortLevel(level);
            return (temperature, reasoningEffortLevel);
        }

        if (settings.Temperature.HasValue)
        {
            temperature = settings.Temperature;
        }

        if (string.IsNullOrEmpty(level))
        {
            level = settings?.EffortLevel;
            if (settings?.Parameters != null
                && settings.Parameters.TryGetValue("EffortLevel", out var settingValue)
                && !string.IsNullOrEmpty(settingValue?.Default))
            {
                level = settingValue.Default;
            }
        }

        reasoningEffortLevel = ParseReasoningEffortLevel(level);
        return (temperature, reasoningEffortLevel);
    }

    private ChatReasoningEffortLevel? ParseReasoningEffortLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        return new ChatReasoningEffortLevel(level.ToLower());
    }

    private ChatImageDetailLevel ParseChatImageDetailLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return ChatImageDetailLevel.Auto;
        }

        var imageLevel = ChatImageDetailLevel.Auto;
        switch (level.ToLower())
        {
            case "low":
                imageLevel = ChatImageDetailLevel.Low;
                break;
            case "high":
                imageLevel = ChatImageDetailLevel.High;
                break;
            default:
                break;
        }

        return imageLevel;
    }
    #endregion
}
