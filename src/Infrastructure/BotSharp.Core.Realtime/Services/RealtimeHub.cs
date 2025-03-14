namespace BotSharp.Core.Realtime.Services;

public class RealtimeHub : IRealtimeHub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    private RealtimeHubConnection _conn;
    public RealtimeHubConnection HubConn => _conn;

    private IRealTimeCompletion _completer;
    public IRealTimeCompletion Completer => _completer;

    public RealtimeHub(IServiceProvider services, ILogger<RealtimeHub> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task Listen(WebSocket userWebSocket, 
        Action<string> onUserMessageReceived)
    {
        var buffer = new byte[1024 * 16];
        WebSocketReceiveResult result;
        

        do
        {
            result = await userWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);

            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            onUserMessageReceived(receivedText);

            if (_conn.Event == "user_connected")
            {
                await ConnectToModel(userWebSocket);
            }
            else if (_conn.Event == "user_data_received")
            {
                await _completer.AppenAudioBuffer(_conn.Data);
            }
            else if (_conn.Event == "user_dtmf_received")
            {
                await HandleUserDtmfReceived();
            }
            else if (_conn.Event == "user_disconnected")
            {
                await _completer.Disconnect();
                await HandleUserDisconnected();
            }
        } while (!result.CloseStatus.HasValue);

        await userWebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ConnectToModel(WebSocket userWebSocket)
    {
        var hookProvider = _services.GetRequiredService<ConversationHookProvider>();
        var convService = _services.GetRequiredService<IConversationService>();
        convService.SetConversationId(_conn.ConversationId, []);
        var conversation = await convService.GetConversation(_conn.ConversationId);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);
        _conn.CurrentAgentId = agent.Id;

        // Set model
        var model = agent.LlmConfig.Model;
        if (!model.Contains("-realtime-"))
        {
            var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
            model = llmProviderService.GetProviderModel("openai", "gpt-4", realTime: true).Name;
        }

        _completer.SetModelName(model);
        _conn.Model = model;

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.Push(agent.Id);

        var storage = _services.GetRequiredService<IConversationStorage>();
        var dialogs = convService.GetDialogHistory();
        if (dialogs.Count == 0)
        {
            dialogs.Add(new RoleDialogModel(AgentRole.User, "Hi"));
            storage.Append(_conn.ConversationId, dialogs.First());
        }

        routing.Context.SetDialogs(dialogs);

        var states = _services.GetRequiredService<IConversationStateService>();

        await _completer.Connect(_conn, 
            onModelReady: async () => 
            {
                if (states.ContainsState("init_audio_file"))
                {
                    await _completer.UpdateSession(_conn, turnDetection: true);
                }
                else
                {
                    // Control initial session, prevent initial response interruption
                    await _completer.UpdateSession(_conn, turnDetection: false);

                    if (dialogs.LastOrDefault()?.Role == AgentRole.Assistant)
                    {
                        await _completer.TriggerModelInference($"Rephase your last response:\r\n{dialogs.LastOrDefault()?.Content}");
                    }
                    else
                    {
                        await _completer.TriggerModelInference("Reply based on the conversation context.");
                    }

                    // Start turn detection
                    await Task.Delay(1000 * 8);
                    await _completer.UpdateSession(_conn, turnDetection: true);
                }
            },
            onModelAudioDeltaReceived: async (audioDeltaData, itemId) =>
            {
                var data = _conn.OnModelMessageReceived(audioDeltaData);
                await SendEventToUser(userWebSocket, data);

                // If this is the first delta of a new response, set the start timestamp
                if (!_conn.ResponseStartTimestamp.HasValue)
                {
                    _conn.ResponseStartTimestamp = _conn.LatestMediaTimestamp;
                    _logger.LogDebug($"Setting start timestamp for new response: {_conn.ResponseStartTimestamp}ms");
                }
                // Record last assistant item ID for interruption handling
                if (!string.IsNullOrEmpty(itemId))
                {
                    _conn.LastAssistantItemId = itemId;
                }

                // Send mark messages to Media Streams so we know if and when AI response playback is finished
                await SendMark(userWebSocket, _conn);
            }, 
            onModelAudioResponseDone: async () =>
            {
                var data = _conn.OnModelAudioResponseDone();
                await SendEventToUser(userWebSocket, data);
            }, 
            onAudioTranscriptDone: async transcript =>
            {

            },
            onModelResponseDone: async messages =>
            {
                foreach (var message in messages)
                {
                    // Invoke function
                    if (message.MessageType == MessageTypeName.FunctionCall &&
                        !string.IsNullOrEmpty(message.FunctionName))
                    {
                        await routing.InvokeFunction(message.FunctionName, message);
                    }
                    else
                    {
                        // append output audio transcript to conversation
                        dialogs.Add(message);
                        storage.Append(_conn.ConversationId, message);

                        foreach (var hook in hookProvider.HooksOrderByPriority)
                        {
                            hook.SetAgent(agent)
                                .SetConversation(conversation);

                            await hook.OnResponseGenerated(message);
                        }
                    }
                }
            },
            onConversationItemCreated: async response =>
            {
                
            },
            onInputAudioTranscriptionCompleted: async message =>
            {
                // append input audio transcript to conversation
                dialogs.Add(message);
                storage.Append(_conn.ConversationId, message);

                foreach (var hook in hookProvider.HooksOrderByPriority)
                {
                    hook.SetAgent(agent)
                        .SetConversation(conversation);

                    await hook.OnMessageReceived(message);
                }
            },
            onUserInterrupted: async () =>
            {
                // Reset states
                _conn.ResetResponseState();

                var data = _conn.OnModelUserInterrupted();
                await SendEventToUser(userWebSocket, data);
            });
    }

    private async Task SendMark(WebSocket userWebSocket, RealtimeHubConnection conn)
    {
        if (!string.IsNullOrEmpty(conn.StreamId))
        {
            var markEvent = new
            {
                @event = "mark",
                streamSid = conn.StreamId,
                mark = new { name = "responsePart" }
            };
            await SendEventToUser(userWebSocket, markEvent);
            conn.MarkQueue.Enqueue("responsePart");
        }
    }

    private async Task HandleUserDtmfReceived()
    {
        var routing = _services.GetRequiredService<IRoutingService>();
        var hookProvider = _services.GetRequiredService<ConversationHookProvider>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(_conn.CurrentAgentId);
        var dialogs = routing.Context.GetDialogs();
        var convService = _services.GetRequiredService<IConversationService>();
        var conversation = await convService.GetConversation(_conn.ConversationId);

        var message = new RoleDialogModel(AgentRole.User, _conn.Data)
        {
            CurrentAgentId = routing.Context.GetCurrentAgentId()
        };
        dialogs.Add(message);

        var storage = _services.GetRequiredService<IConversationStorage>();
        storage.Append(_conn.ConversationId, message);

        foreach (var hook in hookProvider.HooksOrderByPriority)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            await hook.OnMessageReceived(message);
        }

        await _completer.InsertConversationItem(message);
        var instruction = await _completer.UpdateSession(_conn);
        await _completer.TriggerModelInference($"{instruction}\r\n\r\nReply based on the user input: {message.Content}");
    }

    private async Task HandleUserDisconnected()
    {

    }

    private async Task SendEventToUser(WebSocket webSocket, object message)
    {
        var data = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public RealtimeHubConnection SetHubConnection(string conversationId)
    {
        _conn = new RealtimeHubConnection
        {
            ConversationId = conversationId
        };

        return _conn;
    }

    public IRealTimeCompletion SetCompleter(string provider)
    {
        _completer = _services.GetServices<IRealTimeCompletion>().First(x => x.Provider == provider);
        return _completer;
    }
}
