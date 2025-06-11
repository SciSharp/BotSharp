using BotSharp.Abstraction.Hooks;
using BotSharp.Plugin.OpenAI.Models.Realtime;
using OpenAI.Chat;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime;

/// <summary>
/// Reference to https://platform.openai.com/docs/api-reference/realtime-server-events
/// </summary>
public class RealTimeCompletionProvider : IRealTimeCompletion
{
    public string Provider => "openai";
    public string Model => _model;

    private readonly IServiceProvider _services;
    private readonly ILogger<RealTimeCompletionProvider> _logger;
    private readonly BotSharpOptions _botsharpOptions;

    private string _model = "gpt-4o-mini-realtime-preview";
    private LlmRealtimeSession _session;
    private bool _isBlocking = false;

    private RealtimeHubConnection _conn;
    private Func<Task> _onModelReady;
    private Func<string, string, Task> _onModelAudioDeltaReceived;
    private Func<Task> _onModelAudioResponseDone;
    private Func<string, Task> _onModelAudioTranscriptDone;
    private Func<List<RoleDialogModel>, Task> _onModelResponseDone;
    private Func<string, Task> _onConversationItemCreated;
    private Func<RoleDialogModel, Task> _onInputAudioTranscriptionDone;
    private Func<Task> _onInterruptionDetected;

    public RealTimeCompletionProvider(
        IServiceProvider services,
        ILogger<RealTimeCompletionProvider> logger,
        BotSharpOptions botsharpOptions)
    {
        _logger = logger;
        _services = services;
        _botsharpOptions = botsharpOptions;
    }

    public async Task Connect(
        RealtimeHubConnection conn,
        Func<Task> onModelReady,
        Func<string, string, Task> onModelAudioDeltaReceived,
        Func<Task> onModelAudioResponseDone,
        Func<string, Task> onModelAudioTranscriptDone,
        Func<List<RoleDialogModel>, Task> onModelResponseDone,
        Func<string, Task> onConversationItemCreated,
        Func<RoleDialogModel, Task> onInputAudioTranscriptionDone,
        Func<Task> onInterruptionDetected)
    {
        _logger.LogInformation($"Connecting {Provider} realtime server...");

        _conn = conn;
        _onModelReady = onModelReady;
        _onModelAudioDeltaReceived = onModelAudioDeltaReceived;
        _onModelAudioResponseDone = onModelAudioResponseDone;
        _onModelAudioTranscriptDone = onModelAudioTranscriptDone;
        _onModelResponseDone = onModelResponseDone;
        _onConversationItemCreated = onConversationItemCreated;
        _onInputAudioTranscriptionDone = onInputAudioTranscriptionDone;
        _onInterruptionDetected = onInterruptionDetected;

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var realtimeSettings = _services.GetRequiredService<RealtimeModelSettings>();

        _model = realtimeSettings.Model;
        var settings = settingsService.GetSetting(Provider, _model);

        _session?.Dispose();
        _session = new LlmRealtimeSession(_services, new ChatSessionOptions
        {
            Provider = Provider,
            JsonOptions = _botsharpOptions.JsonSerializerOptions,
            Logger = _logger
        });

        await _session.ConnectAsync(
            uri: new Uri($"wss://api.openai.com/v1/realtime?model={_model}"),
            headers: new Dictionary<string, string>
            {
                {"Authorization", $"Bearer {settings.ApiKey}"},
                {"OpenAI-Beta", "realtime=v1"}
            },
            cancellationToken: CancellationToken.None);

        _ = ReceiveMessage(realtimeSettings);
    }

    private async Task ReceiveMessage(RealtimeModelSettings realtimeSettings)
    {
        DateTime? startTime = null;

        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var response = JsonSerializer.Deserialize<ServerEventResponse>(receivedText);

            if (realtimeSettings?.ModelResponseTimeoutSeconds > 0
                && !string.IsNullOrWhiteSpace(realtimeSettings?.ModelResponseTimeoutEndEvent)
                && startTime.HasValue
                && (DateTime.UtcNow - startTime.Value).TotalSeconds >= realtimeSettings.ModelResponseTimeoutSeconds
                && response.Type != realtimeSettings.ModelResponseTimeoutEndEvent)
            {
                startTime = null;
                await TriggerModelInference("Responsd to user immediately");
                continue;
            }

            if (response.Type == "error")
            {
                _logger.LogError($"{response.Type}: {receivedText}");
                var error = JsonSerializer.Deserialize<ServerEventErrorResponse>(receivedText);
                if (error?.Body.Type == "server_error")
                {
                    break;
                }
            }
            else if (response.Type == "session.created")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                _isBlocking = false;
                await _onModelReady();
            }
            else if (response.Type == "session.updated")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
            }
            else if (response.Type == "response.audio_transcript.delta")
            {
                _logger.LogDebug($"{response.Type}: {receivedText}");
            }
            else if (response.Type == "response.audio_transcript.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                var data = JsonSerializer.Deserialize<ResponseAudioTranscript>(receivedText);
                await _onModelAudioTranscriptDone(data.Transcript);
            }
            else if (response.Type == "response.audio.delta")
            {
                var audio = JsonSerializer.Deserialize<ResponseAudioDelta>(receivedText);
                if (audio?.Delta != null)
                {
                    _logger.LogDebug($"{response.Type}: {receivedText}");
                    await _onModelAudioDeltaReceived(audio.Delta, audio.ItemId);
                }
            }
            else if (response.Type == "response.audio.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                await _onModelAudioResponseDone();
            }
            else if (response.Type == "response.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                var data = JsonSerializer.Deserialize<ResponseDone>(receivedText).Body;
                if (data.Status != "completed")
                {
                    if (data.StatusDetails.Type == "incomplete" && data.StatusDetails.Reason == "max_output_tokens")
                    {
                        await _onInterruptionDetected();
                        await TriggerModelInference("Response user concisely");
                    }
                }
                else
                {
                    var messages = await OnResponsedDone(_conn, receivedText);
                    await _onModelResponseDone(messages);
                }
            }
            else if (response.Type == "conversation.item.created")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");

                var data = JsonSerializer.Deserialize<ConversationItemCreated>(receivedText);
                if (data?.Item?.Role == "user")
                {
                    startTime = DateTime.UtcNow;
                }

                await _onConversationItemCreated(receivedText);
            }
            else if (response.Type == "conversation.item.input_audio_transcription.completed")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");

                var message = await OnUserAudioTranscriptionCompleted(_conn, receivedText);
                if (!string.IsNullOrEmpty(message.Content))
                {
                    await _onInputAudioTranscriptionDone(message);
                }
            }
            else if (response.Type == "input_audio_buffer.speech_started")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                // Handle user interuption
                await _onInterruptionDetected();
            }
            else if (response.Type == "input_audio_buffer.speech_stopped")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
            }
            else if (response.Type == "input_audio_buffer.committed")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
            }
        }

        _session.Dispose();
    }


    public async Task Reconnect(RealtimeHubConnection conn)
    {
        _logger.LogInformation($"Reconnecting {Provider} realtime server...");

        _isBlocking = true;
        _conn = conn;
        await Disconnect();
        await Task.Delay(500);
        await Connect(
                _conn,
                _onModelReady,
                _onModelAudioDeltaReceived,
                _onModelAudioResponseDone,
                _onModelAudioTranscriptDone,
                _onModelResponseDone,
                _onConversationItemCreated,
                _onInputAudioTranscriptionDone,
                _onInterruptionDetected);
    }

    public async Task Disconnect()
    {
        _logger.LogInformation($"Disconnecting {Provider} realtime server...");

        if (_session != null)
        {
            await _session.DisconnectAsync();
            _session.Dispose();
        }
    }

    public async Task AppenAudioBuffer(string message)
    {
        if (_isBlocking) return;

        var audioAppend = new
        {
            type = "input_audio_buffer.append",
            audio = message
        };

        await SendEventToModel(audioAppend);
    }

    public async Task AppenAudioBuffer(ArraySegment<byte> data, int length)
    {
        if (_isBlocking) return;

        var message = Convert.ToBase64String(data.AsSpan(0, length).ToArray());
        await AppenAudioBuffer(message);
    }

    public async Task TriggerModelInference(string? instructions = null)
    {
        // Triggering model inference
        if (!string.IsNullOrEmpty(instructions))
        {
            await SendEventToModel(new
            {
                type = "response.create",
                response = new
                {
                    instructions
                }
            });
        }
        else
        {
            await SendEventToModel(new
            {
                type = "response.create"
            });
        }
    }

    public async Task CancelModelResponse()
    {
        await SendEventToModel(new
        {
            type = "response.cancel"
        });
    }

    public async Task RemoveConversationItem(string itemId)
    {
        await SendEventToModel(new
        {
            type = "conversation.item.delete",
            item_id = itemId
        });
    }

    public async Task SendEventToModel(object message)
    {
        if (_session == null) return;

        await _session.SendEventToModelAsync(message);
    }

    public async Task<string> UpdateSession(RealtimeHubConnection conn, bool isInit = false)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var conv = await convService.GetConversation(conn.ConversationId);
        var agent = await agentService.LoadAgent(conn.CurrentAgentId);
        var (prompt, messages, options) = PrepareOptions(agent, []);

        var instruction = messages.FirstOrDefault()?.Content.FirstOrDefault()?.Text ?? agent?.Description ?? string.Empty;
        var functions = options.Tools.Select(x =>
        {
            var fn = new FunctionDef
            {
                Name = x.FunctionName,
                Description = x.FunctionDescription
            };
            fn.Parameters = JsonSerializer.Deserialize<FunctionParametersDef>(x.FunctionParameters);
            return fn;
        }).ToArray();

        var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();
        var sessionUpdate = new
        {
            type = "session.update",
            session = new RealtimeSessionUpdateRequest
            {
                InputAudioFormat = realtimeModelSettings.InputAudioFormat,
                OutputAudioFormat = realtimeModelSettings.OutputAudioFormat,
                Voice = realtimeModelSettings.Voice,
                Instructions = instruction,
                ToolChoice = "auto",
                Tools = functions,
                Modalities = realtimeModelSettings.Modalities,
                Temperature = Math.Max(options.Temperature ?? realtimeModelSettings.Temperature, 0.6f),
                MaxResponseOutputTokens = realtimeModelSettings.MaxResponseOutputTokens,
                TurnDetection = new RealtimeSessionTurnDetection
                {
                    InterruptResponse = realtimeModelSettings.InterruptResponse/*,
                    Threshold = realtimeModelSettings.TurnDetection.Threshold,
                    PrefixPadding = realtimeModelSettings.TurnDetection.PrefixPadding,
                    SilenceDuration = realtimeModelSettings.TurnDetection.SilenceDuration*/
                },
                InputAudioNoiseReduction = new InputAudioNoiseReduction
                {
                    Type = "near_field"
                }
            }
        };

        if (realtimeModelSettings.InputAudioTranscribe)
        {
            var words = new List<string>();
            HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)), agent.Id);

            sessionUpdate.session.InputAudioTranscription = new InputAudioTranscription
            {
                Model = realtimeModelSettings.InputAudioTranscription.Model,
                Language = realtimeModelSettings.InputAudioTranscription.Language,
                Prompt = string.Join(", ", words.Select(x => x.ToLower().Trim()).Distinct()).SubstringMax(1024)
            };
        }

        await HookEmitter.Emit<IContentGeneratingHook>(_services, async hook =>
        {
            await hook.OnSessionUpdated(agent, instruction, functions, isInit: false);
        }, agent.Id);

        await SendEventToModel(sessionUpdate);
        await Task.Delay(300);
        return instruction;
    }

    public async Task InsertConversationItem(RoleDialogModel message)
    {
        if (message.Role == AgentRole.Function)
        {
            var functionConversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    call_id = message.ToolCallId,
                    type = "function_call_output",
                    output = message.Content
                }
            };

            await SendEventToModel(functionConversationItem);
        }
        else if (message.Role == AgentRole.Assistant)
        {
            var conversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = message.Role,
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = message.Content
                        }
                    }
                }
            };

            await SendEventToModel(conversationItem);
        }
        else if (message.Role == AgentRole.User)
        {
            var conversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = message.Role,
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = message.Content
                        }
                    }
                }
            };

            await SendEventToModel(conversationItem);
        }
        else
        {
            throw new NotImplementedException($"Unrecognized role {message.Role}.");
        }
    }

    
    public void SetModelName(string model)
    {
        _model = model;
    }

    #region Private methods
    private async Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response)
    {
        var outputs = new List<RoleDialogModel>();

        var data = JsonSerializer.Deserialize<ResponseDone>(response).Body;
        if (data.Status != "completed")
        {
            _logger.LogError(data.StatusDetails.ToString());
            /*if (data.StatusDetails.Type == "incomplete" && data.StatusDetails.Reason == "max_output_tokens")
            {
                await TriggerModelInference("Response user concisely");
            }*/
            return [];
        }

        var prompts = new List<string>();
        var inputTokenDetails = data.Usage?.InputTokenDetails;
        var outputTokenDetails = data.Usage?.OutputTokenDetails;

        foreach (var output in data.Outputs)
        {
            if (output.Type == "function_call")
            {
                outputs.Add(new RoleDialogModel(AgentRole.Assistant, output.Arguments)
                {
                    CurrentAgentId = conn.CurrentAgentId,
                    FunctionName = output.Name,
                    FunctionArgs = output.Arguments,
                    ToolCallId = output.CallId,
                    MessageId = output.Id,
                    MessageType = MessageTypeName.FunctionCall
                });

                prompts.Add($"{output.Name}({output.Arguments})");
            }
            else if (output.Type == "message")
            {
                var content = output.Content.FirstOrDefault()?.Transcript ?? string.Empty;

                outputs.Add(new RoleDialogModel(output.Role, content)
                {
                    CurrentAgentId = conn.CurrentAgentId,
                    MessageId = output.Id,
                    MessageType = MessageTypeName.Plain
                });

                prompts.Add(content);
            }
        }


        // After chat completion hook
        var text = string.Join("\r\n", prompts);
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(conn.CurrentAgentId);

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = conn.CurrentAgentId
            },
            new TokenStatsModel
            {
                Provider = Provider,
                Model = _model,
                Prompt = text,
                TextInputTokens = inputTokenDetails?.TextTokens ?? 0 - inputTokenDetails?.CachedTokenDetails?.TextTokens ?? 0,
                CachedTextInputTokens = data.Usage?.InputTokenDetails?.CachedTokenDetails?.TextTokens ?? 0,
                AudioInputTokens = inputTokenDetails?.AudioTokens ?? 0 - inputTokenDetails?.CachedTokenDetails?.AudioTokens ?? 0,
                CachedAudioInputTokens = inputTokenDetails?.CachedTokenDetails?.AudioTokens ?? 0,
                TextOutputTokens = outputTokenDetails?.TextTokens ?? 0,
                AudioOutputTokens = outputTokenDetails?.AudioTokens ?? 0
            });
        }

        return outputs;
    }

    private async Task<RoleDialogModel> OnUserAudioTranscriptionCompleted(RealtimeHubConnection conn, string response)
    {
        var data = JsonSerializer.Deserialize<ResponseAudioTranscript>(response);
        return new RoleDialogModel(AgentRole.User, data.Transcript)
        {
            CurrentAgentId = conn.CurrentAgentId
        };
    }

    private (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;

        var messages = new List<ChatMessage>();

        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
                            ? tokens
                            : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;
        var options = new ChatCompletionOptions()
        {
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
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
            var text = agentService.RenderedInstruction(agent);
            messages.Add(new SystemChatMessage(text));
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

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                messages.Add(new AssistantChatMessage(new List<ChatToolCall>
                {
                    ChatToolCall.CreateFunctionToolCall(message.ToolCallId, message.FunctionName, BinaryData.FromString(message.FunctionArgs ?? string.Empty))
                }));

                messages.Add(new ToolChatMessage(message.ToolCallId, message.Content));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                var textPart = ChatMessageContentPart.CreateTextPart(text);
                var contentParts = new List<ChatMessageContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    foreach (var file in message.Files)
                    {
                        if (!string.IsNullOrEmpty(file.FileData))
                        {
                            var (contentType, binary) = FileUtility.GetFileInfoFromData(file.FileData);
                            var contentPart = ChatMessageContentPart.CreateImagePart(binary, contentType, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileStorageUrl))
                        {
                            var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                            var binary = fileStorage.GetFileBytes(file.FileStorageUrl);
                            var contentPart = ChatMessageContentPart.CreateImagePart(binary, contentType, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            var uri = new Uri(file.FileUrl);
                            var contentPart = ChatMessageContentPart.CreateImagePart(uri, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                    }
                }
                messages.Add(new UserChatMessage(contentParts) { ParticipantName = message.FunctionName });
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

            if (!string.IsNullOrEmpty(verbose))
            {
                prompt += $"\r\n[CONVERSATION]\r\n{verbose}\r\n";
            }
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
    #endregion
}