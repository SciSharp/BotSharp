using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Options;
using BotSharp.Abstraction.Realtime.Sessions;
using BotSharp.Core.Infrastructures;
using BotSharp.Core.Session;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace BotSharp.Plugin.ChatHub;

public class ChatStreamMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChatStreamMiddleware> _logger;

    public ChatStreamMiddleware(
        RequestDelegate next,
        ILogger<ChatStreamMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var request = httpContext.Request;

        if (request.Path.StartsWithSegments("/chat/stream"))
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    var services = httpContext.RequestServices;
                    var segments = request.Path.Value!.Split("/");
                    var agentId = segments[segments.Length - 2];
                    var conversationId = segments[segments.Length - 1];

                    /*
                     * Identity, before anything else touches the connection.
                     *
                     * The handshake carries no Authorization header - a browser cannot set one on
                     * a WebSocket - so the signed-in user arrives as a query parameter. It stays
                     * a query parameter rather than a path segment because agentId and
                     * conversationId are read from the END of the path above, so an extra segment
                     * would silently shift both.
                     *
                     * Emitted through the SYNCHRONOUS Emit overload: ambient identity is
                     * AsyncLocal-backed and a write inside an async method does not escape it, so
                     * an awaited hook would establish nothing for the work awaited below.
                     */
                    var userId = httpContext.Request.Query["user-id"].FirstOrDefault();
                    HookEmitter.Emit<IRealtimeHook>(services,
                        hook => hook.OnAuthenticate(agentId, conversationId, userId), agentId);

                    using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    await HandleWebSocket(services, agentId, conversationId, webSocket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error when connecting Chat stream. ({ex.Message})");
                }
                return;
            }
        }

        await _next(httpContext);
    }

    private async Task HandleWebSocket(IServiceProvider services, string agentId, string conversationId, WebSocket webSocket)
    {
        using var session = new BotSharpRealtimeSession(services, webSocket, new ChatSessionOptions
        {
            Provider = "BotSharp Chat Stream",
            BufferSize = 1024 * 32,
            JsonOptions = BotSharpOptions.defaultJsonOptions,
            Logger = _logger
        });

        var hub = services.GetRequiredService<IRealtimeHub>();
        var conn = hub.SetHubConnection(conversationId);
        conn.CurrentAgentId = agentId;
        InitEvents(conn);

        // load conversation and state
        var convService = services.GetRequiredService<IConversationService>();
        var state = services.GetRequiredService<IConversationStateService>();
        var routing = services.GetRequiredService<IRoutingService>();
        await convService.SetConversationId(conversationId, []);
        await convService.GetConversationRecordOrCreateNew(agentId);
        await routing.Context.Push(agentId);

        await foreach (ChatSessionUpdate update in session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var (eventType, data) = MapEvents(conn, receivedText, conversationId);
            if (eventType == "start")
            {
                _logger.LogDebug($"Start chat stream connection for conversation ({conversationId})");
                var request = InitRequest(data, conversationId);
                await ConnectToModel(hub, session, request);
            }
            else if (eventType == "media")
            {
                if (!string.IsNullOrEmpty(data))
                {
                    await hub.Completer.AppenAudioBuffer(data);
                }
            }
            else if (eventType == "disconnect")
            {
                _logger.LogDebug($"Disconnecting chat stream connection for conversation ({conversationId})");
                break;
            }
        }

        /*
         * Runs however the loop ended, not just on an explicit disconnect. The browser sends
         * "disconnect" and closes the socket in the same breath, so the close often wins the
         * race; closing the tab sends nothing at all. Either way the loop simply ends, and
         * leaving the model session undisconnected stranded the final turn in the provider's
         * buffer with nothing left to flush it.
         */
        if (hub.Completer != null)
        {
            try
            {
                await hub.Completer.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when disconnecting the model for conversation ({conversationId})");
            }
        }

        await convService.SaveStates();
        await session.DisconnectAsync();
    }

    private async Task ConnectToModel(IRealtimeHub hub, BotSharpRealtimeSession session, ChatStreamRequest? request)
    {
        await hub.ConnectToModel(responseToUser: async data =>
        {
            if (session != null)
            {
                await session.SendEventAsync(data);
            }
        }, initStates: request?.States, options: request?.Options);
    }

    private (string, string) MapEvents(RealtimeHubConnection conn, string receivedText, string conversationId)
    {
        ChatStreamEventResponse? response = new();

        try
        {
            response = JsonSerializer.Deserialize<ChatStreamEventResponse>(receivedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deserializing chat stream event response for conversation ({conversationId}) (response: {receivedText?.SubstringMax(30)})");
        }
        
        var data = response?.Body?.Payload ?? string.Empty;
        switch (response.Event)
        {
            case "start":
                conn.ResetStreamState();
                break;
            case "media":
                break;
            case "disconnect":
                break;
        }

        return (response.Event, data);
    }

    private void InitEvents(RealtimeHubConnection conn)
    {
        conn.OnModelMessageReceived = message =>
            JsonSerializer.Serialize(new
            {
                @event = "media",
                media = new { payload = message }
            });

        conn.OnModelAudioResponseDone = () =>
            JsonSerializer.Serialize(new
            {
                @event = "mark",
                mark = new { name = "responsePart" }
            });

        conn.OnModelTranscriptDelta = (role, delta) =>
            JsonSerializer.Serialize(new
            {
                @event = "transcript",
                transcript = new { role, delta }
            });

        conn.OnModelUserInterrupted = () =>
            JsonSerializer.Serialize(new
            {
                @event = "clear"
            });
    }

    private ChatStreamRequest? InitRequest(string data, string conversationId)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatStreamRequest>(data, BotSharpOptions.defaultJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deserializing initial request data for conversation ({conversationId}).");
            return null;
        }
    }
}
