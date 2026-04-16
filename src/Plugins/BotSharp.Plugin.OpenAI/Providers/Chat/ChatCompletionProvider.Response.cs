#pragma warning disable OPENAI001
using OpenAI.Responses;

namespace BotSharp.Plugin.OpenAI.Providers.Chat;

public partial class ChatCompletionProvider
{
    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, apiKey: _apiKey, _services);
        var responsesClient = client.GetResponsesClient();
        var (prompt, options) = PrepareResponseOptions(agent, conversations);

        var response = await responsesClient.CreateResponseAsync(options);
        var value = response.Value;

        var functionCall = value.OutputItems.OfType<FunctionCallResponseItem>().FirstOrDefault();
        var reasoningItem = value.OutputItems.OfType<ReasoningResponseItem>().FirstOrDefault();
        var text = value.GetOutputText() ?? string.Empty;
        var thinkingText = reasoningItem?.GetSummaryText();

        RoleDialogModel responseMessage;
        if (functionCall != null)
        {
            _logger.LogInformation($"Action: {nameof(GetChatCompletions)}, Agent: {agent.Name}, ToolCall: {functionCall.FunctionName}");

            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = functionCall.CallId,
                FunctionName = functionCall.FunctionName.NormalizeFunctionName(),
                FunctionArgs = functionCall.FunctionArguments?.ToString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }
        else if (value.IncompleteStatusDetails?.Reason == ResponseIncompleteStatusReason.MaxOutputTokens)
        {
            _logger.LogWarning($"Action: {nameof(GetChatCompletions)}, Reason: {value.IncompleteStatusDetails.Reason}, Agent: {agent.Name}, MaxOutputTokens: {options.MaxOutputTokenCount}, Content:{text}");

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
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }

        if (!string.IsNullOrEmpty(thinkingText))
        {
            responseMessage.MetaData ??= [];
            responseMessage.MetaData[Constants.ThinkingText] = thinkingText;
        }

        var tokenUsage = value?.Usage;
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

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, apiKey: _apiKey, _services);
        var responsesClient = client.GetResponsesClient();
        var (prompt, options) = PrepareResponseOptions(agent, conversations);

        var response = await responsesClient.CreateResponseAsync(options);
        var value = response.Value;

        var functionCall = value.OutputItems.OfType<FunctionCallResponseItem>().FirstOrDefault();
        var reasoningItem = value.OutputItems.OfType<ReasoningResponseItem>().FirstOrDefault();
        var text = value.GetOutputText() ?? string.Empty;
        var thinkingText = reasoningItem?.GetSummaryText();

        RoleDialogModel responseMessage;
        if (functionCall != null)
        {
            _logger.LogInformation($"Action: {nameof(GetChatCompletionsAsync)}, Agent: {agent.Name}, ToolCall: {functionCall.FunctionName}");

            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = functionCall.CallId,
                FunctionName = functionCall.FunctionName.NormalizeFunctionName(),
                FunctionArgs = functionCall.FunctionArguments?.ToString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }
        else if (value.IncompleteStatusDetails?.Reason == ResponseIncompleteStatusReason.MaxOutputTokens)
        {
            _logger.LogWarning($"Action: {nameof(GetChatCompletionsAsync)}, Reason: {value.IncompleteStatusDetails.Reason}, Agent: {agent.Name}, MaxOutputTokens: {options.MaxOutputTokenCount}, Content:{text}");

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
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }

        if (!string.IsNullOrEmpty(thinkingText))
        {
            responseMessage.MetaData ??= [];
            responseMessage.MetaData[Constants.ThinkingText] = thinkingText;
        }

        var tokenUsage = value?.Usage;
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

        if (functionCall != null)
        {
            await onFunctionExecuting(responseMessage);
        }
        else
        {
            await onMessageReceived(responseMessage);
        }

        return true;
    }



    #region Private methods
    private (string, CreateResponseOptions) PrepareResponseOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;
        renderedInstructions = [];

        var maxTokens = int.TryParse(_state.GetState("max_tokens"), out var tokens)
                        ? tokens
                        : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;

        var options = new CreateResponseOptions(_model, new List<ResponseItem>())
        {
            MaxOutputTokenCount = maxTokens
        };

        var (_, reasoningEffortLevel) = ParseReasoning(settings?.Reasoning, agent);
        var responseReasoningLevel = ParseResponseReasoningEffortLevel(reasoningEffortLevel?.ToString());
        if (responseReasoningLevel.HasValue)
        {
            options.ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = responseReasoningLevel.Value,
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto
            };
        }

        // Prepare instruction and functions
        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            renderedInstructions.Add(instruction);
            options.InputItems.Add(ResponseItem.CreateSystemMessageItem(instruction));
        }

        // Render functions as tools
        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function, renderData))
            {
                continue;
            }

            var property = agentService.RenderFunctionProperty(agent, function, renderData);

            options.Tools.Add(ResponseTool.CreateFunctionTool(
                function.Name,
                BinaryData.FromObjectAsJson(property),
                strictModeEnabled: false,
                function.Description));
        }

        AddBuiltInTools(options.Tools, settings);

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            options.InputItems.Add(ResponseItem.CreateSystemMessageItem(agent.Knowledges));
        }

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var sample in samples)
        {
            options.InputItems.Add(sample.Role == AgentRole.User
                ? ResponseItem.CreateUserMessageItem(sample.Content)
                : ResponseItem.CreateAssistantMessageItem(sample.Content));
        }

        var filteredMessages = conversations.Select(x => x).ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        var imageDetailLevel = ResponseImageDetailLevel.Auto;
        if (allowMultiModal)
        {
            imageDetailLevel = ParseResponseImageDetailLevel(_state.GetState("chat_image_detail_level"));
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                var toolCallId = message.ToolCallId.IfNullOrEmptyAs(message.FunctionName);
                options.InputItems.Add(ResponseItem.CreateFunctionCallItem(
                    toolCallId,
                    message.FunctionName,
                    BinaryData.FromString(message.FunctionArgs ?? "{}")));
                options.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(
                    toolCallId,
                    message.LlmContent));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = message.LlmContent;
                var textPart = ResponseContentPart.CreateInputTextPart(text);
                var contentParts = new List<ResponseContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectResponseContentParts(contentParts, message.Files!, imageDetailLevel);
                }
                options.InputItems.Add(ResponseItem.CreateUserMessageItem(contentParts));
            }
            else if (message.Role == AgentRole.Assistant)
            {
                var text = message.LlmContent;
                var textPart = ResponseContentPart.CreateOutputTextPart(text, []);
                var contentParts = new List<ResponseContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectResponseContentParts(contentParts, message.Files!, imageDetailLevel);
                }
                options.InputItems.Add(ResponseItem.CreateAssistantMessageItem(contentParts));
            }
        }

        var prompt = GetResponseApiPrompt(options);
        return (prompt, options);
    }

    private string GetResponseApiPrompt(CreateResponseOptions options)
    {
        var sb = new StringBuilder();
        foreach (var item in options.InputItems)
        {
            if (item is MessageResponseItem msg)
            {
                var text = msg.Content?.FirstOrDefault()?.Text ?? string.Empty;
                sb.AppendLine($"{msg.Role}: {text}");
            }
            else if (item is FunctionCallResponseItem fc)
            {
                sb.AppendLine($"{AgentRole.Assistant}: Call function {fc.FunctionName}({fc.FunctionArguments})");
            }
            else if (item is FunctionCallOutputResponseItem fco)
            {
                sb.AppendLine($"{AgentRole.Function}: {fco.FunctionOutput}");
            }
        }

        if (!options.Tools.IsNullOrEmpty())
        {
            var functions = string.Join("\r\n", options.Tools.OfType<FunctionTool>().Select(fn =>
            {
                return $"\r\n{fn.FunctionName}: {fn.FunctionDescription}\r\n{fn.FunctionParameters}";
            }));
            sb.AppendLine($"\r\n[FUNCTIONS]{functions}\r\n");
        }

        return sb.ToString();
    }

    private ResponseReasoningEffortLevel? ParseResponseReasoningEffortLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        return new ResponseReasoningEffortLevel(level.ToLower());
    }

    private WebSearchToolContextSize? ParseWebSearchContextSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return null;
        }

        return size.ToLower() switch
        {
            "low" => WebSearchToolContextSize.Low,
            "medium" => WebSearchToolContextSize.Medium,
            "high" => WebSearchToolContextSize.High,
            _ => null
        };
    }

    private void CollectResponseContentParts(List<ResponseContentPart> contentParts, List<BotSharpFile> files, ResponseImageDetailLevel imageDetailLevel)
    {
        foreach (var file in files)
        {
            if (!string.IsNullOrEmpty(file.FileData))
            {
                var (contentType, binary) = FileUtility.GetFileInfoFromData(file.FileData);
                contentType = contentType.IfNullOrEmptyAs(file.ContentType);
                var contentPart = IsImageContentType(contentType)
                    ? ResponseContentPart.CreateInputImagePart(binary, imageDetailLevel)
                    : ResponseContentPart.CreateInputFilePart(binary, contentType, file.FileFullName);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileStorageUrl))
            {
                var fileStorage = _services.GetRequiredService<IFileStorageService>();
                var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
                var contentType = FileUtility.GetFileContentType(file.FileStorageUrl).IfNullOrEmptyAs(file.ContentType);
                var contentPart = IsImageContentType(contentType)
                    ? ResponseContentPart.CreateInputImagePart(binary, imageDetailLevel)
                    : ResponseContentPart.CreateInputFilePart(binary, contentType, file.FileFullName);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileUrl))
            {
                var uri = new Uri(file.FileUrl);
                var contentType = FileUtility.GetFileContentType(file.FileUrl).IfNullOrEmptyAs(file.ContentType);
                var contentPart = IsImageContentType(contentType)
                    ? ResponseContentPart.CreateInputImagePart(uri, imageDetailLevel)
                    : ResponseContentPart.CreateInputFilePart(uri);
                contentParts.Add(contentPart);
            }
        }
    }

    private ResponseImageDetailLevel ParseResponseImageDetailLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return ResponseImageDetailLevel.Auto;
        }

        var imageLevel = ResponseImageDetailLevel.Auto;
        var levelLower = level.ToLower();
        switch (levelLower)
        {
            case "low":
                imageLevel = ResponseImageDetailLevel.Low;
                break;
            case "high":
                imageLevel = ResponseImageDetailLevel.High;
                break;
            case "original":
                imageLevel = new ResponseImageDetailLevel(levelLower);
                break;
            default:
                break;
        }

        return imageLevel;
    }


    private void AddBuiltInTools(IList<ResponseTool> tools, LlmModelSetting? modelSettings)
    {
        if (bool.TryParse(_state.GetState("enable_web_search"), out var webSearchEnabled) && webSearchEnabled)
        {
            var (location, contextSize) = ParseWebSearchContext(modelSettings?.WebSearch);
            tools.Add(ResponseTool.CreateWebSearchTool(userLocation: location, searchContextSize: contextSize));
        }
        else if (bool.TryParse(_state.GetState("enable_web_search_preview"), out var webSearchPreviewEnabled) && webSearchPreviewEnabled)
        {
            var (location, contextSize) = ParseWebSearchContext(modelSettings?.WebSearch);
            tools.Add(ResponseTool.CreateWebSearchPreviewTool(userLocation: location, searchContextSize: contextSize));
        }
    }

    private (WebSearchToolLocation?, WebSearchToolContextSize?) ParseWebSearchContext(WebSearchSetting? settings)
    {
        WebSearchToolLocation? webSearchLocation = null;
        WebSearchToolContextSize? webSearchContextSize = null;

        var contextSize = _state.GetState("web_search_context_size");
        if (string.IsNullOrEmpty(contextSize)
            && settings?.Parameters != null
            && settings.Parameters.TryGetValue("SearchContextSize", out var settingValue)
            && !string.IsNullOrEmpty(settingValue?.Default))
        {
            contextSize = settingValue.Default;
        }
        if (string.IsNullOrEmpty(contextSize))
        {
            contextSize = _settings.WebSearch?.SearchContextSize;
        }
        if (string.IsNullOrEmpty(contextSize))
        {
            contextSize = settings?.SearchContextSize;
        }
        webSearchContextSize = ParseWebSearchContextSize(contextSize);

        var userLocation = ResolveWebSearchUserLocation();
        if (userLocation?.HasAnyValue == true)
        {
            webSearchLocation = WebSearchToolLocation.CreateApproximateLocation(
                userLocation.Country,
                userLocation.Region,
                userLocation.City,
                userLocation.Timezone);
        }

        return (webSearchLocation, webSearchContextSize);
    }

    private WebSearchUserLocation? ResolveWebSearchUserLocation()
    {
        var location = WebSearchUserLocation.FromJson(_state.GetState("web_search_user_location"));
        if (location?.HasAnyValue == true)
        {
            return location;
        }

        location = _settings.WebSearch?.UserLocation;
        if (location?.HasAnyValue == true)
        {
            return location;
        }

        return null;
    }
    #endregion
}
