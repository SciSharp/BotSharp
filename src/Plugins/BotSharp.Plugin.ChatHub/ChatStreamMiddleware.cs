using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Realtime.Models.Session;
using BotSharp.Core.Session;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

namespace BotSharp.Plugin.ChatHub;

public class ChatStreamMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChatStreamMiddleware> _logger;
    private BotSharpRealtimeSession _session;

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
                    var segments = request.Path.Value.Split("/");
                    var agentId = segments[segments.Length - 2];
                    var conversationId = segments[segments.Length - 1];

                    using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    await HandleWebSocket(services, agentId, conversationId, webSocket);
                }
                catch (Exception ex)
                {
                    _session?.Dispose();
                    _logger.LogError(ex, $"Error when connecting Chat stream. ({ex.Message})");
                }
                return;
            }
        }

        await _next(httpContext);
    }

    private async Task HandleWebSocket(IServiceProvider services, string agentId, string conversationId, WebSocket webSocket)
    {
        _session?.Dispose();
        _session = new BotSharpRealtimeSession(services, webSocket, new ChatSessionOptions
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
        convService.SetConversationId(conversationId, []);
        await convService.GetConversationRecordOrCreateNew(agentId);

        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var (eventType, data) = MapEvents(conn, receivedText, conversationId);
            if (eventType == "start")
            {
#if DEBUG
                _logger.LogCritical($"Start chat stream connection for conversation ({conversationId})");
#endif
                var request = InitRequest(data, conversationId);
                await ConnectToModel(hub, webSocket, request?.States);
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
#if DEBUG
                _logger.LogCritical($"Disconnecting chat stream connection for conversation ({conversationId})");
#endif
                await hub.Completer.Disconnect();
                break;
            }
        }

        convService.SaveStates();
        await _session.DisconnectAsync();
        _session.Dispose();
    }

    private async Task ConnectToModel(IRealtimeHub hub, WebSocket webSocket, List<MessageState>? states = null)
    {
        await hub.ConnectToModel(responseToUser: async data =>
        {
            if (_session != null)
            {
                await _session.SendEventAsync(data);
            }
        }, initStates: states);
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
