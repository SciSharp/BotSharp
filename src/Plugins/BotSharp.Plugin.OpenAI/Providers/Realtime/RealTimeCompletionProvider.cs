using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.OpenAI.Models.Realtime;
using OpenAI.Chat;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime;

/// <summary>
/// Reference to https://platform.openai.com/docs/api-reference/realtime-server-events
/// </summary>
public class RealTimeCompletionProvider : IRealTimeCompletion
{
    public string Provider => "openai";
    public string Model => _model;

    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<RealTimeCompletionProvider> _logger;

    protected string _model = "gpt-4o-mini-realtime-preview-2024-12-17";
    private ClientWebSocket _webSocket;

    public RealTimeCompletionProvider(
        OpenAiSettings settings,
        ILogger<RealTimeCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task Connect(RealtimeHubConnection conn,
        Action onModelReady,
        Action<string,string> onModelAudioDeltaReceived,
        Action onModelAudioResponseDone,
        Action<string> onModelAudioTranscriptDone,
        Action<List<RoleDialogModel>> onModelResponseDone,
        Action<string> onConversationItemCreated,
        Action<RoleDialogModel> onInputAudioTranscriptionCompleted,
        Action onUserInterrupted)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider: "openai", conn.Model);

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {settings.ApiKey}");
        _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

        await _webSocket.ConnectAsync(new Uri($"wss://api.openai.com/v1/realtime?model={conn.Model}"), CancellationToken.None);

        if (_webSocket.State == WebSocketState.Open)
        {
            // Receive a message
            _ = ReceiveMessage(conn,
                onModelReady,
                onModelAudioDeltaReceived,
                onModelAudioResponseDone,
                onModelAudioTranscriptDone,
                onModelResponseDone,
                onConversationItemCreated,
                onInputAudioTranscriptionCompleted,
                onUserInterrupted);
        }
    }

    public async Task Disconnect()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
        }
    }

    public async Task AppenAudioBuffer(string message)
    {
        var audioAppend = new
        {
            type = "input_audio_buffer.append",
            audio = message
        };

        await SendEventToModel(audioAppend);
    }

    public async Task TriggerModelInference(string? instructions = null)
    {
        // Triggering model inference
        await SendEventToModel(new
        {
            type = "response.create",
            response = new
            {
                instructions
            }
        });
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

    private async Task ReceiveMessage(RealtimeHubConnection conn,
        Action onModelReady,
        Action<string,string> onModelAudioDeltaReceived,
        Action onModelAudioResponseDone,
        Action<string> onModelAudioTranscriptDone,
        Action<List<RoleDialogModel>> onModelResponseDone,
        Action<string> onConversationItemCreated,
        Action<RoleDialogModel> onUserAudioTranscriptionCompleted,
        Action onUserInterrupted)
    {
        var buffer = new byte[1024 * 32];
        WebSocketReceiveResult result;

        do
        {
            result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
            
            // Convert received data to text/audio (Twilio sends Base64-encoded audio)
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }
            _logger.LogDebug($"{nameof(RealTimeCompletionProvider)} received: {receivedText}");
            var response = JsonSerializer.Deserialize<ServerEventResponse>(receivedText);

            if (response.Type == "error")
            {
                var error = JsonSerializer.Deserialize<ServerEventErrorResponse>(receivedText);
                _logger.LogError($"Error: {error.Body.Message}");
            }
            else if (response.Type == "session.created")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                onModelReady();
            }
            else if (response.Type == "session.updated")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
            }
            else if (response.Type == "response.audio_transcript.delta")
            {

            }
            else if (response.Type == "response.audio_transcript.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                var data = JsonSerializer.Deserialize<ResponseAudioTranscript>(receivedText);
                await Task.Delay(1000);
                onModelAudioTranscriptDone(data.Transcript);
            }
            else if (response.Type == "response.audio.delta")
            {
                var audio = JsonSerializer.Deserialize<ResponseAudioDelta>(receivedText);
                if (audio?.Delta != null)
                {
                    _logger.LogDebug($"{response.Type}: {receivedText}");
                    onModelAudioDeltaReceived(audio.Delta, audio.ItemId);
                }
            }
            else if (response.Type == "response.audio.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                onModelAudioResponseDone();
            }
            else if (response.Type == "response.done")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                var messages = await OnResponsedDone(conn, receivedText);
                onModelResponseDone(messages);
            }
            else if (response.Type == "conversation.item.created")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                onConversationItemCreated(receivedText);
            }
            else if (response.Type == "conversation.item.input_audio_transcription.completed")
            {
                _logger.LogInformation($"{response.Type}: {receivedText}");
                var message = await OnUserAudioTranscriptionCompleted(conn, receivedText);
                if (!string.IsNullOrEmpty(message.Content))
                {
                    onUserAudioTranscriptionCompleted(message);
                }
            }
            else if (response.Type == "input_audio_buffer.speech_started")
            {
                // Handle user interuption
                if (conn.MarkQueue.Count > 0 && conn.ResponseStartTimestamp != null)
                {
                    var elapsedTime = conn.LatestMediaTimestamp - conn.ResponseStartTimestamp;

                    if (!string.IsNullOrEmpty(conn.LastAssistantItemId))
                    {
                        var truncateEvent = new
                        {
                            type = "conversation.item.truncate",
                            item_id = conn.LastAssistantItemId,
                            content_index = 0,
                            audio_end_ms = elapsedTime
                        };

                        await SendEventToModel(truncateEvent);
                    }

                    onUserInterrupted();
                }
            }

        } while (!result.CloseStatus.HasValue);

        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    public async Task SendEventToModel(object message)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            return;
        }

        if (message is not string data)
        {
            data = JsonSerializer.Serialize(message, BotSharpOptions.defaultJsonOptions);
        }

        var buffer = Encoding.UTF8.GetBytes(data);
        
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<RealtimeSession> CreateSession(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var instruction = messages.FirstOrDefault()?.Content.FirstOrDefault()?.Text ?? agent.Description;

        var args = new RealtimeSessionCreationRequest
        {
            Model = _model,
            Instructions = instruction,
            ToolChoice = "auto",
            Tools = options.Tools.Select(x =>
            {
                var fn = new FunctionDef
                {
                    Name = x.FunctionName,
                    Description = x.FunctionDescription
                };
                fn.Parameters = JsonSerializer.Deserialize<FunctionParametersDef>(x.FunctionParameters);
                return fn;
            }).ToArray(),
        };

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, args.Model ?? _model);

        var api = _services.GetRequiredService<IOpenAiRealtimeApi>();
        var session = await api.GetSessionAsync(args, settings.ApiKey);
        return session;
    }

    public async Task<string> UpdateSession(RealtimeHubConnection conn, bool turnDetection = true)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conv = await convService.GetConversation(conn.ConversationId);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conn.CurrentAgentId);

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, []);

        var instruction = messages.FirstOrDefault()?.Content.FirstOrDefault()?.Text ?? agent.Description;
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

        var words = new List<string>();
        HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)));

        var sessionUpdate = new
        {
            type = "session.update",
            session = new RealtimeSessionUpdateRequest
            {
                InputAudioFormat = "g711_ulaw",
                OutputAudioFormat = "g711_ulaw",
                InputAudioTranscription = new InputAudioTranscription
                {
                    Model = "whisper-1",
                    Language = "en",
                    Prompt = string.Join(", ", words.Select(x => x.ToLower().Trim()).Distinct()).SubstringMax(1024)
                },
                Voice = "alloy",
                Instructions = instruction,
                ToolChoice = "auto",
                Tools = functions,
                Modalities = [ "text", "audio" ],
                Temperature = Math.Max(options.Temperature ?? 0f, 0.6f),
                MaxResponseOutputTokens = 512,
                TurnDetection = new RealtimeSessionTurnDetection
                {
                    Threshold = 0.9f,
                    PrefixPadding = 300,
                    SilenceDuration = 800
                }
            }
        };

        if (!turnDetection)
        {
            sessionUpdate.session.TurnDetection = null;
        }

        await HookEmitter.Emit<IContentGeneratingHook>(_services, async hook =>
        {
            await hook.OnSessionUpdated(agent, instruction, functions);
        });

        await SendEventToModel(sessionUpdate);

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
            throw new NotImplementedException("");
        }
    }

    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
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
                            var (contentType, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
                            var contentPart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), contentType, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileStorageUrl))
                        {
                            var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                            var bytes = fileStorage.GetFileBytes(file.FileStorageUrl);
                            var contentPart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), contentType, ChatImageDetailLevel.Auto);
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

    public void SetModelName(string model)
    {
        _model = model;
    }

    public async Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response)
    {
        var outputs = new List<RoleDialogModel>();

        var data = JsonSerializer.Deserialize<ResponseDone>(response).Body;
        if (data.Status != "completed")
        {
            return [];
        }

        foreach (var output in data.Outputs)
        {
            if (output.Type == "function_call")
            {
                outputs.Add(new RoleDialogModel(output.Role, output.Arguments)
                {
                    CurrentAgentId = conn.CurrentAgentId,
                    FunctionName = output.Name,
                    FunctionArgs = output.Arguments,
                    ToolCallId = output.CallId,
                    MessageId = output.Id,
                    MessageType = MessageTypeName.FunctionCall
                });
            }
            else if (output.Type == "message")
            {
                var content = output.Content.FirstOrDefault();

                outputs.Add(new RoleDialogModel(output.Role, content.Transcript)
                {
                    CurrentAgentId = conn.CurrentAgentId,
                    MessageId = output.Id,
                    MessageType = MessageTypeName.Plain
                });
            }
        }

        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();
        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, "response.done")
            {
                CurrentAgentId = conn.CurrentAgentId
            }, new TokenStatsModel
            {
                Provider = Provider,
                Model = _model,
                CompletionCount = data.Usage.OutputTokens,
                PromptCount = data.Usage.InputTokens
            });
        }

        return outputs;
    }

    public async Task<RoleDialogModel> OnUserAudioTranscriptionCompleted(RealtimeHubConnection conn, string response)
    {
        var data = JsonSerializer.Deserialize<ResponseAudioTranscript>(response);
        return new RoleDialogModel(AgentRole.User, data.Transcript)
        {
            CurrentAgentId = conn.CurrentAgentId
        };
    }

    public async Task<RoleDialogModel> OnConversationItemCreated(RealtimeHubConnection conn, string response)
    {
        var item = JsonSerializer.Deserialize<ConversationItemCreated>(response).Item;
        var message = new RoleDialogModel(item.Role, item.Content.FirstOrDefault()?.Transcript);

        return message;
    }
}
