#pragma warning disable OPENAI001
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Diagnostics;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Core.Infrastructures.Streams;
using BotSharp.Core.MessageHub;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using static BotSharp.Abstraction.Diagnostics.ModelDiagnostics;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Chat;

public class ChatCompletionProvider : IChatCompletion
{
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ChatCompletionProvider> _logger;
    private List<string> renderedInstructions = [];

    protected string _model;

    public virtual string Provider => "azure-openai";
    public string Model => _model;

    public ChatCompletionProvider(
        AzureOpenAiSettings settings,
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);
        var convService = _services.GetService<IConversationStateService>();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        ClientResult<ChatCompletion>? response = null;
        ChatCompletion value = default;
        RoleDialogModel responseMessage;
        using (var activity = ModelDiagnostics.StartCompletionActivity(null, _model, Provider, prompt, convService))
        {
            try
            {
                response = chatClient.CompleteChat(messages, options);
                value = response.Value;

                var reason = value.FinishReason;
                var content = value.Content;
                var text = content.FirstOrDefault()?.Text ?? string.Empty;

                activity?.SetTag(ModelDiagnosticsTags.FinishReason, reason);
                if (reason == ChatFinishReason.FunctionCall || reason == ChatFinishReason.ToolCalls)
                {
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
            }
            catch (ClientResultException ex)
            {
                _logger.LogError(ex, ex.Message);
                responseMessage = new RoleDialogModel(AgentRole.Assistant, "The response was filtered due to the prompt triggering our content management policy. Please modify your prompt and retry.")
                {
                    CurrentAgentId = agent.Id,
                    MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                    RenderedInstruction = string.Join("\r\n", renderedInstructions)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                responseMessage = new RoleDialogModel(AgentRole.Assistant, ex.Message)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                    RenderedInstruction = string.Join("\r\n", renderedInstructions)
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
                    TextInputTokens = (tokenUsage?.InputTokenCount ?? 0) - (inputTokenDetails?.CachedTokenCount ?? 0),
                    CachedTextInputTokens = inputTokenDetails?.CachedTokenCount ?? 0,
                    TextOutputTokens = tokenUsage?.OutputTokenCount ?? 0
                });
            }
            activity?.SetTag("output", responseMessage.Content);

            return responseMessage;
        }
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
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

        var client = ProviderHelper.GetClient(Provider, _model, _services);
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

    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
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

        await foreach (var choice in chatClient.CompleteChatStreamingAsync(messages, options))
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
                _logger.LogCritical($"Content update: {text}");
#endif

                var content = new RoleDialogModel(AgentRole.Assistant, text)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = messageId
                };
                hub.Push(new()
                {
                    EventName = ChatEvent.OnReceiveLlmStreamMessage,
                    RefId = conv.ConversationId,
                    Data = content
                });
            }

            if (choice.FinishReason == ChatFinishReason.ToolCalls || choice.FinishReason == ChatFinishReason.FunctionCall)
            {
                var meta = toolCalls.FirstOrDefault(x => !string.IsNullOrEmpty(x.FunctionName));
                var functionName = meta?.FunctionName;
                var toolCallId = meta?.ToolCallId;
                var args = toolCalls.Where(x => x.FunctionArgumentsUpdate != null).Select(x => x.FunctionArgumentsUpdate.ToString()).ToList();
                var functionArgument = string.Join(string.Empty, args);

#if DEBUG
                _logger.LogCritical($"Tool Call (id: {toolCallId}) => {functionName}({functionArgument})");
#endif

                responseMessage = new RoleDialogModel(AgentRole.Function, string.Empty)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = messageId,
                    ToolCallId = toolCallId,
                    FunctionName = functionName,
                    FunctionArgs = functionArgument
                };
            }
            else if (choice.FinishReason.HasValue)
            {
                var allText = textStream.GetText();
                _logger.LogCritical($"Text Content: {allText}");

                responseMessage = new RoleDialogModel(AgentRole.Assistant, allText)
                {
                    CurrentAgentId = agent.Id,
                    MessageId = messageId,
                    IsStreaming = true
                };
            }
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

    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
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
            imageDetailLevel = ParseChatImageDetailLevel(state.GetState("chat_image_detail_level"));
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
                var contentPart = ChatMessageContentPart.CreateImagePart(binary, contentType.IfNullOrEmptyAs(file.ContentType), imageDetailLevel);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileStorageUrl))
            {
                var fileStorage = _services.GetRequiredService<IFileStorageService>();
                var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
                var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                var contentPart = ChatMessageContentPart.CreateImagePart(binary, contentType.IfNullOrEmptyAs(file.ContentType), imageDetailLevel);
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileUrl))
            {
                var uri = new Uri(file.FileUrl);
                var contentPart = ChatMessageContentPart.CreateImagePart(uri, imageDetailLevel);
                contentParts.Add(contentPart);
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
        var state = _services.GetRequiredService<IConversationStateService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);

        // Reasoning effort
        ChatReasoningEffortLevel? reasoningEffortLevel = null;
        float? temperature = float.Parse(state.GetState("temperature", "0.0"));
        if (settings?.Reasoning != null)
        {
            temperature = settings.Reasoning.Temperature;
            var level = state.GetState("reasoning_effort_level")
                         .IfNullOrEmptyAs(agent?.LlmConfig?.ReasoningEffortLevel)
                         .IfNullOrEmptyAs(settings?.Reasoning?.EffortLevel);
            reasoningEffortLevel = ParseReasoningEffortLevel(level);
        }

        // Web search
        ChatWebSearchOptions? webSearchOptions = null;
        if (settings?.WebSearch != null)
        {
            temperature = null;
            reasoningEffortLevel = null;
            webSearchOptions = new();
        }

        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
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

    private ChatReasoningEffortLevel? ParseReasoningEffortLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        var effortLevel = new ChatReasoningEffortLevel("minimal");
        switch (level.ToLower())
        {
            case "low":
                effortLevel = ChatReasoningEffortLevel.Low;
                break;
            case "medium":
                effortLevel = ChatReasoningEffortLevel.Medium;
                break;
            case "high":
                effortLevel = ChatReasoningEffortLevel.High;
                break;
            default:
                break;
        }

        return effortLevel;
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

    public void SetModelName(string model)
    {
        _model = model;
    }
}
