using BotSharp.Abstraction.Realtime;
using System.Net.WebSockets;
using System;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Realtime;

public class RealtimeHub : IRealtimeHub
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public RealtimeHub(IServiceProvider services, ILogger<RealtimeHub> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task Listen(WebSocket userWebSocket, 
        Func<string, RealtimeHubConnection> onUserMessageReceived)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result;

        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        var model = llmProviderService.GetProviderModel("openai", "gpt-4",
            realTime: true).Name;

        var completer = _services.GetServices<IRealTimeCompletion>().First(x => x.Provider == "openai");
        completer.SetModelName(model);

        do
        {
            result = await userWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _logger.LogDebug($"Received from user: {receivedText}");
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var conn = onUserMessageReceived(receivedText);
            conn.Model = model;

            if (conn.Event == "user_connected")
            {
                await ConnectToModel(completer, userWebSocket, conn);
            }
            else if (conn.Event == "user_data_received")
            {
                await completer.AppenAudioBuffer(conn.Data);
            }
            else if (conn.Event == "user_disconnected")
            {
                await completer.Disconnect();
            }
        } while (!result.CloseStatus.HasValue);

        await userWebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ConnectToModel(IRealTimeCompletion completer, WebSocket userWebSocket, RealtimeHubConnection conn)
    {
        var hookProvider = _services.GetRequiredService<ConversationHookProvider>();
        var storage = _services.GetRequiredService<IConversationStorage>();
        var convService = _services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conn.ConversationId, []);
        var conversation = await convService.GetConversation(conn.ConversationId);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conversation.AgentId);
        var routing = _services.GetRequiredService<IRoutingService>();
        var dialogs = convService.GetDialogHistory();
        routing.Context.SetDialogs(dialogs);

        await completer.Connect(conn, 
            onModelReady: async () => 
            {
                // Control initial session
                var data = await completer.UpdateInitialSession(conn);
                await completer.SendEventToModel(data);

                // Add dialog history
                foreach (var item in dialogs)
                {
                    var dialogItem = await completer.InsertConversationItem(item);
                    await completer.SendEventToModel(data);
                }

                if (dialogs.LastOrDefault()?.Role == AgentRole.Assistant)
                {
                    await completer.TriggerModelInference($"Rephase your last response:\r\n{dialogs.LastOrDefault()?.Content}");
                }
                else
                {
                    await completer.TriggerModelInference("Reply based on the conversation context.");
                }
            },
            onModelAudioDeltaReceived: async audioDeltaData =>
            {
                var data = conn.OnModelMessageReceived(audioDeltaData);
                await SendEventToUser(userWebSocket, data);
            }, 
            onModelAudioResponseDone: async () =>
            {
                var data = conn.OnModelAudioResponseDone();
                await SendEventToUser(userWebSocket, data);
            }, 
            onAudioTranscriptDone: async transcript =>
            {
                var message = new RoleDialogModel(AgentRole.Assistant, transcript);

                // append transcript to conversation
                storage.Append(conn.ConversationId, message);

                foreach (var hook in hookProvider.HooksOrderByPriority)
                {
                    hook.SetAgent(agent)
                        .SetConversation(conversation);

                    if (!string.IsNullOrEmpty(transcript))
                    {
                        await hook.OnMessageReceived(message);
                    }
                }
            },
            onModelResponseDone: async response =>
            {
                var messages = await completer.OnResponsedDone(conn, response);
                foreach (var message in messages)
                {
                    // Invoke function
                    if (message.FunctionName != null)
                    {
                        await routing.InvokeFunction(message.FunctionName, message);
                        var data = await completer.InsertConversationItem(message);
                        await completer.SendEventToModel(data);
                        await completer.TriggerModelInference("Reply based on the function's output.");
                    }
                }
            },
            onUserInterrupted: async () =>
            {
                var data = conn.OnModelUserInterrupted();
                await SendEventToUser(userWebSocket, data);
            });
    }

    private async Task SendEventToUser(WebSocket webSocket, object message)
    {
        var data = JsonSerializer.Serialize(message);
        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
