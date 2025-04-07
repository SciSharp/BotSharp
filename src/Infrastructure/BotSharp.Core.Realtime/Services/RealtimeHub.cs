using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Options;
using BotSharp.Core.Infrastructures;

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

    public async Task ConnectToModel(Func<string, Task> responseToUser)
    {
        var hookProvider = _services.GetService<ConversationHookProvider>();
        var convService = _services.GetRequiredService<IConversationService>();
        convService.SetConversationId(_conn.ConversationId, []);
        var conversation = await convService.GetConversation(_conn.ConversationId);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);
        _conn.CurrentAgentId = agent.Id;

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
        routing.Context.SetMessageId(_conn.ConversationId, dialogs.Last().MessageId);

        var states = _services.GetRequiredService<IConversationStateService>();
        var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();

        await _completer.Connect(_conn, 
            onModelReady: async () => 
            {
                // Not TriggerModelInference, waiting for user utter.
                var instruction = await _completer.UpdateSession(_conn);

                await HookEmitter.Emit<IRealtimeHook>(_services, async hook => await hook.OnModeReady(agent, _completer));
            },
            onModelAudioDeltaReceived: async (audioDeltaData, itemId) =>
            {
                var data = _conn.OnModelMessageReceived(audioDeltaData);
                await responseToUser(data);

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
                // await SendMark(userWebSocket, _conn);
            }, 
            onModelAudioResponseDone: async () =>
            {
                var data = _conn.OnModelAudioResponseDone();
                await responseToUser(data);
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
                        if (message.FunctionName == "route_to_agent")
                        {
                            var instruction = JsonSerializer.Deserialize<FunctionCallFromLlm>(message.FunctionArgs, BotSharpOptions.defaultJsonOptions);
                            await HookEmitter.Emit<IRoutingHook>(_services, async hook => await hook.OnRoutingInstructionReceived(instruction, message));
                        }

                        await routing.InvokeFunction(message.FunctionName, message);
                    }
                    else
                    {
                        // append output audio transcript to conversation
                        dialogs.Add(message);
                        storage.Append(_conn.ConversationId, message);

                        foreach (var hook in hookProvider?.HooksOrderByPriority ?? [])
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
                routing.Context.SetMessageId(_conn.ConversationId, message.MessageId);

                foreach (var hook in hookProvider?.HooksOrderByPriority ?? [])
                {
                    hook.SetAgent(agent)
                        .SetConversation(conversation);

                    await hook.OnMessageReceived(message);
                }
            },
            onUserInterrupted: async () =>
            {
                if (realtimeModelSettings.InterruptResponse)
                {
                    // Reset states
                    _conn.ResetResponseState();

                    var data = _conn.OnModelUserInterrupted();
                    await responseToUser(data);
                }
            });
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
