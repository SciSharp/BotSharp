using Anthropic.SDK.Common;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Core.Infrastructures.Streams;
using BotSharp.Core.MessageHub;
using System.Text.Json.Nodes;

namespace BotSharp.Plugin.AnthropicAI.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "anthropic";
    public string Model => _model;

    protected readonly AnthropicSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    private readonly IConversationStateService _state;

    private List<string> renderedInstructions = [];

    protected string _model;

    public ChatCompletionProvider(
        AnthropicSettings settings,
        IServiceProvider services,
        ILogger<ChatCompletionProvider> logger,
        IConversationStateService state)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
        _state = state;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetAnthropicClient(Provider, _model, _services);
        var (prompt, parameters) = PrepareOptions(agent, conversations);

        var response = await client.Messages.GetClaudeMessageAsync(parameters);

        RoleDialogModel responseMessage;

        if (response.StopReason == StopReason.ToolUse)
        {
            var toolCall = response.ToolCalls.FirstOrDefault();
            responseMessage = new RoleDialogModel(AgentRole.Function, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = toolCall?.Id,
                FunctionName = toolCall?.Name,
                FunctionArgs = toolCall?.Arguments?.ToJsonString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }
        else
        {
            var message = response.FirstMessage;
            responseMessage = new RoleDialogModel(AgentRole.Assistant, message?.Text ?? string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };
        }

        var tokenUsage = response.Usage;

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = tokenUsage?.InputTokens ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokens ?? 0
            });
        }

        return responseMessage;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetAnthropicClient(Provider, _model, _services);
        var (prompt, parameters) = PrepareOptions(agent, conversations);

        var response = await client.Messages.GetClaudeMessageAsync(parameters);

        RoleDialogModel responseMessage;

        if (response.StopReason == StopReason.ToolUse)
        {
            var toolCall = response.ToolCalls.FirstOrDefault();
            responseMessage = new RoleDialogModel(AgentRole.Function, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = toolCall?.Id,
                FunctionName = toolCall?.Name,
                FunctionArgs = toolCall?.Arguments?.ToJsonString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };

            // Execute functions
            await onFunctionExecuting(responseMessage);
        }
        else
        {
            var message = response.FirstMessage;
            responseMessage = new RoleDialogModel(AgentRole.Assistant, message?.Text ?? string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                RenderedInstruction = string.Join("\r\n", renderedInstructions)
            };

            await onMessageReceived(responseMessage);
        }

        var tokenUsage = response.Usage;

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = tokenUsage?.InputTokens ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokens ?? 0
            });
        }

        return true;
    }

    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var client = ProviderHelper.GetAnthropicClient(Provider, _model, _services);
        var (prompt, parameters) = PrepareOptions(agent, conversations, useStream: true);

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
        Usage? tokenUsage = null;

        var responseMessage = new RoleDialogModel(AgentRole.Assistant, string.Empty)
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId
        };

        await foreach (var choice in client.Messages.StreamClaudeMessageAsync(parameters))
        {
            var startMsg = choice.StreamStartMessage;
            var contentBlock = choice.ContentBlock;
            var delta = choice.Delta;

            tokenUsage = delta?.Usage ?? startMsg?.Usage ?? choice.Usage;

            if (delta != null)
            {
                if (delta.StopReason == StopReason.ToolUse)
                {
                    var toolCall = choice.ToolCalls.FirstOrDefault();
                    responseMessage = new RoleDialogModel(AgentRole.Function, string.Empty)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        ToolCallId = toolCall?.Id,
                        FunctionName = toolCall?.Name,
                        FunctionArgs = toolCall?.Arguments?.ToString()?.IfNullOrEmptyAs("{}") ?? "{}"
                    };

#if DEBUG
                    _logger.LogDebug($"Tool Call (id: {toolCall?.Id}) => {toolCall?.Name}({toolCall?.Arguments})");
#endif
                }
                else if (delta.StopReason == StopReason.EndTurn)
                {
                    var allText = textStream.GetText();
                    responseMessage = new RoleDialogModel(AgentRole.Assistant, allText)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        IsStreaming = true
                    };

#if DEBUG
                    _logger.LogDebug($"Stream text Content: {allText}");
#endif
                }
                else if (!string.IsNullOrEmpty(delta.StopReason))
                {
                    responseMessage = new RoleDialogModel(AgentRole.Assistant, delta.StopReason)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId,
                        IsStreaming = true
                    };
                }
                else
                {
                    var deltaText = delta.Text ?? string.Empty;
                    textStream.Collect(deltaText);

                    hub.Push(new()
                    {
                        EventName = ChatEvent.OnReceiveLlmStreamMessage,
                        RefId = conv.ConversationId,
                        Data = new RoleDialogModel(AgentRole.Assistant, deltaText)
                        {
                            CurrentAgentId = agent.Id,
                            MessageId = messageId
                        }
                    });
                }
            }
        }

        hub.Push(new()
        {
            EventName = ChatEvent.AfterReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = responseMessage
        });

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = tokenUsage?.InputTokens ?? 0,
                TextOutputTokens = tokenUsage?.OutputTokens ?? 0
            });
        }

        return responseMessage;
    }

    private (string, MessageParameters) PrepareOptions(Agent agent, List<RoleDialogModel> conversations, bool useStream = false)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;
        renderedInstructions = [];

        var parameters = new MessageParameters()
        {
            Model = _model,
            Stream = useStream,
            Tools = new List<Anthropic.SDK.Common.Tool>(),
            Thinking = GetThinkingParams(settings)
        };

        // Prepare instruction and functions
        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            parameters.System = new List<SystemMessage>()
            {
                new SystemMessage(instruction)
            };
            renderedInstructions.Add(instruction);
        }

        var tools = new List<Anthropic.SDK.Common.Tool>();
        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function, renderData))
            {
                continue;
            }

            var property = agentService.RenderFunctionProperty(agent, function, renderData);
            var jsonArgs = property != null ? JsonSerializer.Serialize(property, BotSharpOptions.defaultJsonOptions) : "{}";
            tools.Add(new Function(function.Name, function.Description, JsonNode.Parse(jsonArgs)));
        }

        var messages = new List<Message>();
        var filteredMessages = conversations.Select(x => x).ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.User)
            {
                var contentParts = new List<ContentBase>();
                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectMessageContentParts(contentParts, message.Files);
                }
                contentParts.Add(new TextContent() { Text = message.LlmContent });
                messages.Add(new Message
                {
                    Role = RoleType.User,
                    Content = contentParts
                });
            }
            else if (message.Role == AgentRole.Assistant)
            {
                var contentParts = new List<ContentBase>();
                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    CollectMessageContentParts(contentParts, message.Files);
                }
                contentParts.Add(new TextContent() { Text = message.LlmContent });
                messages.Add(new Message
                {
                    Role = RoleType.Assistant,
                    Content = contentParts
                });
            }
            else if (message.Role == AgentRole.Function)
            {
                messages.Add(new Message
                {
                    Role = RoleType.Assistant,
                    Content = new List<ContentBase>
                    {
                        new ToolUseContent()
                        {
                            Id = message.ToolCallId,
                            Name = message.FunctionName,
                            Input = JsonNode.Parse(message.FunctionArgs ?? "{}")
                        }
                    }
                });

                messages.Add(new Message()
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new ToolResultContent()
                        {
                            ToolUseId = message.ToolCallId,
                            Content = [new TextContent() { Text = message.LlmContent }]
                        }
                    }
                });
            }
        }

        var temperature = decimal.Parse(_state.GetState("temperature", "0.0"));
        var maxTokens = int.TryParse(_state.GetState("max_tokens"), out var tokens)
            ? tokens
            : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;

        parameters.Messages = messages;
        parameters.Tools = tools;
        parameters.Temperature = temperature;
        parameters.MaxTokens = maxTokens;

        var prompt = GetPrompt(parameters);
        return (prompt, parameters);
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private ThinkingParameters? GetThinkingParams(LlmModelSetting? settings)
    {
        if (settings?.Reasoning?.Parameters == null)
        {
            return null;
        }

        var thinking = new ThinkingParameters();
        var param = settings.Reasoning.Parameters!;

        var bt = _state.GetState("budget_tokens");
        if (int.TryParse(bt, out var budgetTokens)
            || (param.TryGetValue("BudgetTokens", out var value)
            && int.TryParse(value.Default, out budgetTokens)))
        {
            thinking.BudgetTokens = budgetTokens;
        }

        var enableInterleavedThinking = _state.GetState("use_interleaved_thinking");
        if (bool.TryParse(enableInterleavedThinking, out var useInterleavedThinking)
            || (param.TryGetValue("UseInterleavedThinking", out value)
            && bool.TryParse(value.Default, out useInterleavedThinking)))
        {
            thinking.UseInterleavedThinking = useInterleavedThinking;
        }

        return thinking.BudgetTokens > 0 ? thinking : null;
    }

    private string GetPrompt(MessageParameters parameters)
    {
        var prompt = $"{string.Join("\r\n", (parameters.System ?? new List<SystemMessage>()).Select(x => x.Text))}\r\n";
        prompt += "\r\n[CONVERSATION]";

        var verbose = string.Join("\r\n", parameters.Messages
            .Select(x =>
            {
                var role = x.Role.ToString().ToLower();

                if (x.Role == RoleType.User)
                {
                    var content = string.Join("\r\n", x.Content.Select(c =>
                    {
                        if (c is TextContent text)
                            return text.Text;
                        else if (c is ToolResultContent tool)
                            return $"{tool.Content}";
                        else
                            return string.Empty;
                    }));
                    return $"{role}: {content}";
                }
                else if (x.Role == RoleType.Assistant)
                {
                    var content = string.Join("\r\n", x.Content.Select(c =>
                    {
                        if (c is TextContent text)
                            return text.Text;
                        else if (c is ToolUseContent tool)
                            return $"Call function {tool.Name}({JsonSerializer.Serialize(tool.Input)})";
                        else
                            return string.Empty;
                    }));
                    return $"{role}: {content}";
                }

                return string.Empty;
            }));

        prompt += $"\r\n{verbose}\r\n";

        if (parameters.Tools != null && parameters.Tools.Count > 0)
        {
            var functions = string.Join("\r\n",
                parameters.Tools.Select(x =>
                {
                    return $"\r\n{x.Function.Name}: {x.Function.Description}\r\n{JsonSerializer.Serialize(x.Function.Parameters)}";
                }));
            prompt += $"\r\n[FUNCTIONS]\r\n{functions}\r\n";
        }

        return prompt;
    }

    private void CollectMessageContentParts(List<ContentBase> contentParts, List<BotSharpFile> files)
    {
        foreach (var file in files)
        {
            if (!string.IsNullOrEmpty(file.FileData))
            {
                var (contentType, binary) = FileUtility.GetFileInfoFromData(file.FileData);
                var contentPart = new ImageContent
                {
                    Source = new ImageSource
                    {
                        MediaType = contentType,
                        Data = Convert.ToBase64String(binary.ToArray())
                    }
                };
                contentParts.Add(contentPart);
            }
            else if (!string.IsNullOrEmpty(file.FileStorageUrl))
            {
                var fileStorage = _services.GetRequiredService<IFileStorageService>();
                var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
                var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                var contentPart = new ImageContent
                {
                    Source = new ImageSource
                    {
                        MediaType = contentType,
                        Data = Convert.ToBase64String(binary)
                    }
                };
                contentParts.Add(contentPart);
            }
        }
    }
}