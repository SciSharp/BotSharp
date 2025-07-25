using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Utilities;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models.Stream;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio;

/// <summary>
/// Reference to https://github.com/twilio-samples/speech-assistant-openai-realtime-api-node/blob/main/index.js
/// </summary>
public class TwilioStreamMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TwilioStreamMiddleware> _logger;

    public TwilioStreamMiddleware(RequestDelegate next, ILogger<TwilioStreamMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var request = httpContext.Request;

        if (request.Path.StartsWithSegments("/twilio/stream"))
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var services = httpContext.RequestServices;
                var parts = request.Path.Value.Split("/");
                var agentId = parts[3];
                var conversationId = parts[4];
                using WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    await HandleWebSocket(services, agentId, conversationId, webSocket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in WebSocket communication: {ex.Message} for conversation {conversationId}");
                }
                return;
            }
        }

        await _next(httpContext);
    }

    private async Task HandleWebSocket(IServiceProvider services, string agentId, string conversationId, WebSocket webSocket)
    {
        var settings = services.GetRequiredService<RealtimeModelSettings>();
        var hub = services.GetRequiredService<IRealtimeHub>();
        var conn = hub.SetHubConnection(conversationId);
        conn.CurrentAgentId = agentId;

        // load conversation and state
        var convService = services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, []);
        var hooks = services.GetHooks<ITwilioSessionHook>(agentId);
        foreach (var hook in hooks)
        {
            await hook.OnStreamingStarted(conn);
        }
        convService.States.Save();

        var routing = services.GetRequiredService<IRoutingService>();
        routing.Context.Push(agentId);

        var buffer = new byte[1024 * 32];
        WebSocketReceiveResult result;

        do
        {
            Array.Clear(buffer, 0, buffer.Length);
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);

            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var (eventType, data) = MapEvents(conn, receivedText, conversationId);

            if (eventType == "user_connected")
            {
#if DEBUG
                _logger.LogCritical($"Start twilio stream connection for conversation ({conversationId})");
#else
                _logger.LogInformation($"Start twilio stream connection for conversation ({conversationId})");
#endif
                // Connect to model
                await ConnectToModel(hub, webSocket);
            }
            else if (eventType == "user_data_received")
            {
                await hub.Completer.AppenAudioBuffer(data);
            }
            else if (eventType == "user_dtmf_receiving")
            {
                // Send a Stop command to Twilio
                string clearEvent = JsonSerializer.Serialize(new
                {
                    @event = "clear",
                    streamSid = conn.StreamId
                });
                await SendEventToUser(webSocket, clearEvent);
            }
            else if (eventType == "user_dtmf_received")
            {
                await HandleUserDtmfReceived(services, conn, hub.Completer, data);
            }
            else if (eventType == "user_disconnected")
            {
#if DEBUG
                _logger.LogCritical($"Disconnecting twilio stream connection for conversation ({conversationId})");
#else
                _logger.LogInformation($"Disconnecting twilio stream connection for conversation ({conversationId})");
#endif
                await hub.Completer.Disconnect();
                await HandleUserDisconnected();
            }
        } while (!result.CloseStatus.HasValue);

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ConnectToModel(IRealtimeHub hub, WebSocket webSocket)
    {
        await hub.ConnectToModel(async data =>
        {
            await SendEventToUser(webSocket, data);
        });
    }

    private (string, string) MapEvents(RealtimeHubConnection conn, string receivedText, string conversationId)
    {
        StreamEventResponse? response = new();

        try
        {
            response = JsonSerializer.Deserialize<StreamEventResponse>(receivedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deserializing twilio stream event response for conversation ({conversationId}) (response: {receivedText?.SubstringMax(30)})");
        }

        conn.StreamId = response.StreamSid;
        string eventType = response.Event;
        string data = string.Empty;

        switch (response.Event)
        {
            case "start":
                eventType = "user_connected";
                var startResponse = JsonSerializer.Deserialize<StreamEventStartResponse>(receivedText);
                conn.UserSessionId = startResponse.Body.CallSid;
                data = JsonSerializer.Serialize(startResponse.Body.CustomParameters);
                conn.ResetStreamState();
                break;
            case "media":
                eventType = "user_data_received";
                var mediaResponse = JsonSerializer.Deserialize<StreamEventMediaResponse>(receivedText);
                conn.LatestMediaTimestamp = long.Parse(mediaResponse.Body.Timestamp);
                data = mediaResponse.Body.Payload;
                break;
            case "stop":
                eventType = "user_disconnected";
                break;
            case "dtmf":
                var dtmfResponse = JsonSerializer.Deserialize<StreamEventDtmfResponse>(receivedText);
                if (dtmfResponse.Body.Digit == "#")
                {
                    eventType = "user_dtmf_received";
                    data = conn.KeypadInputBuffer;
                    conn.KeypadInputBuffer = string.Empty;
                }
                else
                {
                    eventType = "user_dtmf_receiving";
                    conn.KeypadInputBuffer += dtmfResponse.Body.Digit;
                }
                break;
            default:
                eventType = response.Event;
                break;
        }

        conn.OnModelMessageReceived = message =>
            JsonSerializer.Serialize(new
            {
                @event = "media",
                streamSid = response.StreamSid,
                media = new { payload = message }
            });

        conn.OnModelAudioResponseDone = () =>
            JsonSerializer.Serialize(new
            {
                @event = "mark",
                streamSid = response.StreamSid,
                mark = new { name = "responsePart" }
            });

        conn.OnModelUserInterrupted = () =>
            JsonSerializer.Serialize(new
            {
                @event = "clear",
                streamSid = response.StreamSid
            });

        return (eventType, data);
    }

    private async Task HandleUserDisconnected()
    {

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
            var message = JsonSerializer.Serialize(markEvent);
            await SendEventToUser(userWebSocket, message);
        }
    }

    private async Task SendEventToUser(WebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task HandleUserDtmfReceived(IServiceProvider _services, RealtimeHubConnection conn, IRealTimeCompletion completer, string data)
    {
        var routing = _services.GetRequiredService<IRoutingService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(conn.CurrentAgentId);
        var dialogs = routing.Context.GetDialogs();
        var convService = _services.GetRequiredService<IConversationService>();
        var conversation = await convService.GetConversation(conn.ConversationId);

        var message = new RoleDialogModel(AgentRole.User, data)
        {
            CurrentAgentId = routing.Context.GetCurrentAgentId()
        };
        dialogs.Add(message);

        var storage = _services.GetRequiredService<IConversationStorage>();
        storage.Append(conn.ConversationId, message);

        var hooks = _services.GetHooksOrderByPriority<IConversationHook>(conn.CurrentAgentId);
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent)
                .SetConversation(conversation);

            await hook.OnMessageReceived(message);
        }

        await completer.InsertConversationItem(message);
        await completer.TriggerModelInference($"Response based on the user input: {message.Content}");
    }
}
