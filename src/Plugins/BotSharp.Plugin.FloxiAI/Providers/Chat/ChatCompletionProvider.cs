using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Core.MessageHub;

namespace BotSharp.Plugin.FloxiAI.Providers.Chat;

/// <summary>
/// Chat completion over the floxi inference network. The request is assembled as a plain
/// OpenAI-compatible body rather than through an SDK client, because the transport that carries it is
/// not always HTTP — inside the floxi service the same body is handed to a connected node over the
/// daemon control plane (see <see cref="IFloxiChatTransport"/>).
/// </summary>
public class ChatCompletionProvider : IChatCompletion
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ChatCompletionProvider> _logger;

    protected string _model = string.Empty;
    protected string? _apiKey;

    private List<string> _renderedInstructions = [];

    public virtual string Provider => FloxiAiConstants.ProviderName;
    public string Model => _model;
    public string? ApiKey => _apiKey;

    public ChatCompletionProvider(
        IServiceProvider services,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var (prompt, payload) = PreparePayload(agent, conversations);
        var transport = ResolveTransport();
        var result = await transport.SendChatAsync(_model, payload);

        if (!result.IsSuccessStatusCode)
        {
            // The body carries the backend's own diagnostic, which is for operators, not for the caller.
            _logger.LogError("Model {Model} on {Provider} returned {Status} (served by {ServedBy}): {Body}",
                _model, Provider, result.StatusCode, result.ServedBy ?? "unknown", result.Body);

            throw new InvalidOperationException(
                $"Model '{_model}' on {Provider} rejected the request (status {result.StatusCode}).");
        }

        var completion = ParseCompletion(result.Body);

        RoleDialogModel responseMessage;
        if (completion.ToolCallName != null)
        {
            responseMessage = new RoleDialogModel(AgentRole.Function, completion.Text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                ToolCallId = completion.ToolCallId,
                FunctionName = completion.ToolCallName.NormalizeFunctionName(),
                FunctionArgs = completion.ToolCallArguments,
                RenderedInstruction = string.Join("\r\n", _renderedInstructions)
            };
        }
        else if (completion.FinishReason == "length")
        {
            _logger.LogWarning("Action: {Action}, Reason: {Reason}, Agent: {Agent}, Content: {Content}",
                nameof(GetChatCompletions), completion.FinishReason, agent.Name, completion.Text);

            responseMessage = new RoleDialogModel(AgentRole.Assistant, "AI response exceeded max output length")
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                StopCompletion = true
            };
        }
        else
        {
            if (string.IsNullOrEmpty(completion.Text) && completion.ReasoningLength > 0)
            {
                // Thinking was on and consumed the whole budget, so the model produced reasoning and no
                // answer. Worth naming: an empty reply otherwise looks like the model had nothing to say.
                _logger.LogWarning(
                    "Model {Model} returned {ReasoningLength} characters of reasoning and empty content — " +
                    "chain-of-thought is on for this model, so the output budget went to reasoning.",
                    _model, completion.ReasoningLength);
            }

            responseMessage = new RoleDialogModel(AgentRole.Assistant, completion.Text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
                RenderedInstruction = string.Join("\r\n", _renderedInstructions)
            };
        }

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = completion.PromptTokens - completion.CachedPromptTokens,
                CachedTextInputTokens = completion.CachedPromptTokens,
                TextOutputTokens = completion.CompletionTokens
            });
        }

        return responseMessage;
    }

    /// <summary>
    /// The reply is produced whole and then handed over, either as an answer or as a function call.
    /// </summary>
    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var message = await GetChatCompletions(agent, conversations);

        if (message.Role == AgentRole.Function)
        {
            await onFunctionExecuting(message);
        }
        else
        {
            await onMessageReceived(message);
        }

        return true;
    }

    /// <summary>
    /// Streaming: each delta is pushed to the chat hub as it arrives and the assembled reply is
    /// returned. A transport whose control plane carries one terminal reply per dispatch cannot stream;
    /// that reply is then sent as a single chunk, so a client waiting on stream events still gets them.
    /// </summary>
    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var hub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        var conv = _services.GetRequiredService<IConversationService>();
        var messageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;

        var transport = ResolveTransport();
        if (!transport.SupportsStreaming)
        {
            return await StreamWholeReply(agent, conversations, hub, conv.ConversationId, messageId);
        }

        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var (prompt, payload) = PreparePayload(agent, conversations, stream: true);

        Push(hub, conv.ConversationId, ChatEvent.BeforeReceiveLlmStreamMessage,
            new RoleDialogModel(AgentRole.Assistant, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId
            });

        var cancellation = _services.GetRequiredService<IConversationCancellationService>();
        var aggregate = new FloxiStreamAggregate();

        try
        {
            var token = cancellation.GetToken(conv.ConversationId);

            await foreach (var chunk in transport.SendChatStreamAsync(_model, payload, token))
            {
                var delta = CollectChunk(chunk, aggregate);
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                Push(hub, conv.ConversationId, ChatEvent.OnReceiveLlmStreamMessage,
                    new RoleDialogModel(AgentRole.Assistant, delta)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId
                    });
            }
        }
        catch (OperationCanceledException)
        {
            // Whatever arrived before the stop is still a reply, and the client has already rendered it.
            _logger.LogWarning("Streaming was cancelled for conversation {ConversationId}", conv.ConversationId);
        }

        var responseMessage = BuildStreamedMessage(agent, aggregate, messageId);

        Push(hub, conv.ConversationId, ChatEvent.AfterReceiveLlmStreamMessage, responseMessage);

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                TextInputTokens = aggregate.PromptTokens - aggregate.CachedPromptTokens,
                CachedTextInputTokens = aggregate.CachedPromptTokens,
                TextOutputTokens = aggregate.CompletionTokens
            });
        }

        return responseMessage;
    }

    /// <summary>
    /// Streams a reply that was produced whole: the stream events still bracket it, so the client sees
    /// the sequence it is waiting for, arriving in one piece.
    /// </summary>
    private async Task<RoleDialogModel> StreamWholeReply(
        Agent agent,
        List<RoleDialogModel> conversations,
        MessageHub<HubObserveData<RoleDialogModel>> hub,
        string conversationId,
        string messageId)
    {
        Push(hub, conversationId, ChatEvent.BeforeReceiveLlmStreamMessage,
            new RoleDialogModel(AgentRole.Assistant, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId
            });

        // Runs its own generating hooks, so this path adds none of its own.
        var message = await GetChatCompletions(agent, conversations);

        if (message.Role != AgentRole.Function)
        {
            message.IsStreaming = true;

            if (!string.IsNullOrEmpty(message.Content))
            {
                Push(hub, conversationId, ChatEvent.OnReceiveLlmStreamMessage,
                    new RoleDialogModel(AgentRole.Assistant, message.Content)
                    {
                        CurrentAgentId = agent.Id,
                        MessageId = messageId
                    });
            }
        }

        Push(hub, conversationId, ChatEvent.AfterReceiveLlmStreamMessage, message);

        return message;
    }

    private RoleDialogModel BuildStreamedMessage(Agent agent, FloxiStreamAggregate aggregate, string messageId)
    {
        if (aggregate.ToolCallName != null)
        {
            return new RoleDialogModel(AgentRole.Function, aggregate.Text.ToString())
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId,
                ToolCallId = aggregate.ToolCallId,
                FunctionName = aggregate.ToolCallName.NormalizeFunctionName(),
                FunctionArgs = aggregate.ToolCallArguments.Length > 0 ? aggregate.ToolCallArguments.ToString() : null,
                RenderedInstruction = string.Join("\r\n", _renderedInstructions)
            };
        }

        var text = aggregate.Text.ToString();

        if (aggregate.FinishReason == "length")
        {
            // Reported rather than replaced: the client has already rendered what arrived, so swapping in
            // a notice would erase the partial answer it is showing.
            _logger.LogWarning("Action: {Action}, Reason: {Reason}, Agent: {Agent}",
                nameof(GetChatCompletionsStreamingAsync), aggregate.FinishReason, agent.Name);
        }
        else if (string.IsNullOrEmpty(text) && aggregate.ReasoningLength > 0)
        {
            _logger.LogWarning(
                "Model {Model} streamed {ReasoningLength} characters of reasoning and empty content - " +
                "chain-of-thought is on for this model, so the output budget went to reasoning.",
                _model, aggregate.ReasoningLength);
        }

        return new RoleDialogModel(AgentRole.Assistant, text)
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId,
            IsStreaming = true,
            RenderedInstruction = string.Join("\r\n", _renderedInstructions)
        };
    }

    private static void Push(
        MessageHub<HubObserveData<RoleDialogModel>> hub,
        string conversationId,
        string eventName,
        RoleDialogModel data)
    {
        hub.Push(new()
        {
            EventName = eventName,
            RefId = conversationId,
            Data = data
        });
    }

    #region Request

    private (string, string) PreparePayload(Agent agent, List<RoleDialogModel> conversations, bool stream = false)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;

        _renderedInstructions = [];
        var messages = new JsonArray();

        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            _renderedInstructions.Add(instruction);
            messages.Add(TextMessage(AgentRole.System, instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(TextMessage(AgentRole.System, agent.Knowledges));
        }

        // A dialog that opens with an assistant or tool turn has no request for the model to answer;
        // drop everything before the first user turn so the body starts where the exchange does.
        var filteredMessages = conversations.ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                var toolCallId = message.ToolCallId.IfNullOrEmptyAs(message.FunctionName) ?? string.Empty;

                messages.Add(new JsonObject
                {
                    ["role"] = AgentRole.Assistant,
                    ["content"] = null,
                    ["tool_calls"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = toolCallId,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = message.FunctionName,
                                ["arguments"] = message.FunctionArgs ?? "{}"
                            }
                        }
                    }
                });

                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = toolCallId,
                    ["content"] = message.LlmContent
                });
            }
            else
            {
                var role = message.Role == AgentRole.User ? AgentRole.User : AgentRole.Assistant;
                var hasFiles = allowMultiModal && !message.Files.IsNullOrEmpty();

                messages.Add(hasFiles
                    ? new JsonObject
                    {
                        ["role"] = role,
                        ["content"] = BuildContentParts(message.LlmContent, message.Files!)
                    }
                    : TextMessage(role, message.LlmContent));
            }
        }

        var payload = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = messages,
            ["stream"] = stream,
            ["max_tokens"] = int.TryParse(state.GetState("max_tokens"), out var maxTokens)
                ? maxTokens
                : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN,
            ["temperature"] = settings?.Reasoning?.Temperature
                ?? (float.TryParse(state.GetState("temperature", "0.0"), out var temperature) ? temperature : 0f)
        };

        if (stream)
        {
            // A stream carries no usage block unless it is asked for, and without it every streamed turn
            // would be logged as zero tokens.
            payload["stream_options"] = new JsonObject { ["include_usage"] = true };
        }

        // The qwen chat templates on this network gate chain-of-thought on this flag, and with it left
        // to the server default a thinking model spends the output budget on reasoning_content and
        // returns empty content. Sent explicitly so the default is an answer rather than a deliberation;
        // a caller that wants the reasoning turns it back on through conversation state.
        if (!IsThinkingEnabled(state))
        {
            payload["chat_template_kwargs"] = new JsonObject { ["enable_thinking"] = false };
        }

        var tools = BuildTools(agentService, agent, functions, renderData);
        if (tools.Count > 0)
        {
            payload["tools"] = tools;
        }

        return (GetPrompt(messages, functions), payload.ToJsonString());
    }

    private static bool IsThinkingEnabled(IConversationStateService state)
    {
        return bool.TryParse(state.GetState(FloxiAiConstants.EnableThinkingState), out var enabled) && enabled;
    }

    private static JsonObject TextMessage(string role, string content)
    {
        return new JsonObject
        {
            ["role"] = role,
            ["content"] = content
        };
    }

    /// <summary>
    /// Text first, then one image_url part per file. A file already carrying a data url is passed
    /// through as-is; a stored file is read and encoded; an external url is sent as a url for the
    /// backend to fetch.
    /// </summary>
    private JsonArray BuildContentParts(string text, List<BotSharpFile> files)
    {
        var parts = new JsonArray
        {
            new JsonObject { ["type"] = "text", ["text"] = text }
        };

        foreach (var file in files)
        {
            var url = ResolveImageUrl(file);
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            parts.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = url }
            });
        }

        return parts;
    }

    private string? ResolveImageUrl(BotSharpFile file)
    {
        if (!string.IsNullOrEmpty(file.FileData))
        {
            return file.FileData;
        }

        if (!string.IsNullOrEmpty(file.FileStorageUrl))
        {
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
            var contentType = FileUtility.GetFileContentType(file.FileStorageUrl).IfNullOrEmptyAs(file.ContentType);
            return $"data:{contentType};base64,{Convert.ToBase64String(binary.ToArray())}";
        }

        return string.IsNullOrEmpty(file.FileUrl) ? null : file.FileUrl;
    }

    private JsonArray BuildTools(
        IAgentService agentService,
        Agent agent,
        IEnumerable<FunctionDef> functions,
        IDictionary<string, object> renderData)
    {
        var tools = new JsonArray();

        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function, renderData))
            {
                continue;
            }

            var property = agentService.RenderFunctionProperty(agent, function, renderData);

            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = function.Name,
                    ["description"] = function.Description,
                    ["parameters"] = JsonSerializer.SerializeToNode(property)
                }
            });
        }

        return tools;
    }

    /// <summary>
    /// The request rendered for the content log, so a floxi call reads in the log the way an openai one
    /// does. Image parts are named rather than inlined — a base64 grid would bury the log.
    /// </summary>
    private static string GetPrompt(JsonArray messages, IEnumerable<FunctionDef> functions)
    {
        var lines = messages.Select(message =>
        {
            var role = message?["role"]?.GetValue<string>() ?? string.Empty;

            var text = message?["content"] switch
            {
                JsonArray parts => string.Join(" ", parts
                    .Select(part => part?["type"]?.GetValue<string>() == "text"
                        ? part?["text"]?.GetValue<string>()
                        : "[image]")
                    .Where(x => !string.IsNullOrEmpty(x))),
                JsonValue value => value.GetValue<string>(),
                _ => message?["tool_calls"]?.ToJsonString() ?? string.Empty
            };

            return $"{role}: {text}";
        });

        var prompt = string.Join("\r\n", lines);

        var functionList = functions?.ToList() ?? [];
        if (functionList.Count > 0)
        {
            prompt += $"\r\n\r\n[FUNCTIONS]\r\n{JsonSerializer.Serialize(functionList)}";
        }

        return prompt;
    }

    #endregion

    #region Response

    private IFloxiChatTransport ResolveTransport()
    {
        var transport = _services.GetServices<IFloxiChatTransport>()
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        return transport ?? throw new InvalidOperationException(
            $"No {nameof(IFloxiChatTransport)} is registered, so {Provider} has no way to reach the inference network.");
    }

    private static FloxiCompletion ParseCompletion(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var completion = new FloxiCompletion();

        if (root.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];

            if (choice.TryGetProperty("finish_reason", out var finishReason)
                && finishReason.ValueKind == JsonValueKind.String)
            {
                completion.FinishReason = finishReason.GetString();
            }

            if (choice.TryGetProperty("message", out var message))
            {
                completion.Text = ReadContent(message);

                if (message.TryGetProperty("reasoning_content", out var reasoning)
                    && reasoning.ValueKind == JsonValueKind.String)
                {
                    completion.ReasoningLength = reasoning.GetString()?.Length ?? 0;
                }

                if (message.TryGetProperty("tool_calls", out var toolCalls)
                    && toolCalls.ValueKind == JsonValueKind.Array
                    && toolCalls.GetArrayLength() > 0)
                {
                    var toolCall = toolCalls[0];
                    completion.ToolCallId = toolCall.TryGetProperty("id", out var id) ? id.GetString() : null;

                    if (toolCall.TryGetProperty("function", out var function))
                    {
                        completion.ToolCallName = function.TryGetProperty("name", out var name) ? name.GetString() : null;
                        completion.ToolCallArguments = function.TryGetProperty("arguments", out var args)
                            ? (args.ValueKind == JsonValueKind.String ? args.GetString() : args.GetRawText())
                            : null;
                    }
                }
            }
        }

        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            completion.PromptTokens = ReadTokenCount(usage, "prompt_tokens");
            completion.CompletionTokens = ReadTokenCount(usage, "completion_tokens");

            if (usage.TryGetProperty("prompt_tokens_details", out var details)
                && details.ValueKind == JsonValueKind.Object)
            {
                completion.CachedPromptTokens = ReadTokenCount(details, "cached_tokens");
            }
        }

        return completion;
    }

    /// <summary>
    /// Folds one streamed chunk into <paramref name="aggregate"/> and returns the text delta it carried,
    /// if any. A chunk that cannot be read is skipped rather than fatal: losing one delta degrades the
    /// reply, while throwing would abandon a stream the client is already rendering.
    /// </summary>
    private string? CollectChunk(string chunk, FloxiStreamAggregate aggregate)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(chunk);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Discarded an unreadable stream chunk from {Provider}: {Chunk}", Provider, chunk);
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            string? delta = null;

            if (root.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];

                if (choice.TryGetProperty("finish_reason", out var finishReason)
                    && finishReason.ValueKind == JsonValueKind.String)
                {
                    aggregate.FinishReason = finishReason.GetString();
                }

                if (choice.TryGetProperty("delta", out var deltaElement)
                    && deltaElement.ValueKind == JsonValueKind.Object)
                {
                    var text = ReadContent(deltaElement);
                    if (!string.IsNullOrEmpty(text))
                    {
                        aggregate.Text.Append(text);
                        delta = text;
                    }

                    if (deltaElement.TryGetProperty("reasoning_content", out var reasoning)
                        && reasoning.ValueKind == JsonValueKind.String)
                    {
                        // Counted, not forwarded: the client asked for an answer, not for the deliberation.
                        aggregate.ReasoningLength += reasoning.GetString()?.Length ?? 0;
                    }

                    CollectToolCallDelta(deltaElement, aggregate);
                }
            }

            // Sent in its own final chunk, whose choices array is empty.
            if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
            {
                aggregate.PromptTokens = ReadTokenCount(usage, "prompt_tokens");
                aggregate.CompletionTokens = ReadTokenCount(usage, "completion_tokens");

                if (usage.TryGetProperty("prompt_tokens_details", out var details)
                    && details.ValueKind == JsonValueKind.Object)
                {
                    aggregate.CachedPromptTokens = ReadTokenCount(details, "cached_tokens");
                }
            }

            return delta;
        }
    }

    /// <summary>
    /// A streamed tool call arrives split across chunks: the id and name once, then the arguments a
    /// fragment at a time. Only the first call is followed, matching the non-streaming path.
    /// </summary>
    private static void CollectToolCallDelta(JsonElement delta, FloxiStreamAggregate aggregate)
    {
        if (!delta.TryGetProperty("tool_calls", out var toolCalls)
            || toolCalls.ValueKind != JsonValueKind.Array
            || toolCalls.GetArrayLength() == 0)
        {
            return;
        }

        var toolCall = toolCalls[0];

        if (aggregate.ToolCallId == null
            && toolCall.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String)
        {
            aggregate.ToolCallId = id.GetString();
        }

        if (!toolCall.TryGetProperty("function", out var function)
            || function.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (aggregate.ToolCallName == null
            && function.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String)
        {
            aggregate.ToolCallName = name.GetString();
        }

        if (function.TryGetProperty("arguments", out var args) && args.ValueKind == JsonValueKind.String)
        {
            aggregate.ToolCallArguments.Append(args.GetString());
        }
    }

    /// <summary>
    /// Content is a string on every backend the network fronts today, but the OpenAI schema also allows
    /// an array of parts — read both, so a backend that returns parts is not mistaken for an empty reply.
    /// </summary>
    private static string ReadContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
        {
            return string.Empty;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var texts = content.EnumerateArray()
                .Where(part => part.ValueKind == JsonValueKind.Object && part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrEmpty(text));

            return string.Concat(texts);
        }

        return string.Empty;
    }

    private static long ReadTokenCount(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : 0;
    }

    /// <summary>
    /// A streamed reply under assembly: the chunks each carry a fragment, and only the whole run of them
    /// adds up to a message.
    /// </summary>
    private class FloxiStreamAggregate
    {
        public StringBuilder Text { get; } = new();
        public StringBuilder ToolCallArguments { get; } = new();
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public string? FinishReason { get; set; }
        public int ReasoningLength { get; set; }
        public long PromptTokens { get; set; }
        public long CachedPromptTokens { get; set; }
        public long CompletionTokens { get; set; }
    }

    private class FloxiCompletion
    {
        public string Text { get; set; } = string.Empty;
        public string? FinishReason { get; set; }
        public int ReasoningLength { get; set; }
        public string? ToolCallId { get; set; }
        public string? ToolCallName { get; set; }
        public string? ToolCallArguments { get; set; }
        public long PromptTokens { get; set; }
        public long CachedPromptTokens { get; set; }
        public long CompletionTokens { get; set; }
    }

    #endregion
}
