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
            BufferSize = 1024 * 16,
            JsonOptions = BotSharpOptions.defaultJsonOptions
        });

        var hub = services.GetRequiredService<IRealtimeHub>();
        var conn = hub.SetHubConnection(conversationId);
        conn.CurrentAgentId = agentId;

        // load conversation and state
        var convService = services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, []);
        await convService.GetConversationRecordOrCreateNew(agentId);

        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            var (eventType, data) = MapEvents(conn, receivedText);
            if (eventType == "start")
            {
                await ConnectToModel(hub, webSocket);
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
                await hub.Completer.Disconnect();
                break;
            }
        }


        await _session.Disconnect();
        _session.Dispose();
    }

    private async Task ConnectToModel(IRealtimeHub hub, WebSocket webSocket)
    {
        await hub.ConnectToModel(async data =>
        {
            if (_session != null)
            {
                await _session.SendEvent(data);
            }
        });
    }

    private (string, string) MapEvents(RealtimeHubConnection conn, string receivedText)
    {
        var response = JsonSerializer.Deserialize<ChatStreamEventResponse>(receivedText);
        string data = string.Empty;

        switch (response.Event)
        {
            case "start":
                conn.ResetStreamState();
                break;
            case "media":
                var mediaResponse = JsonSerializer.Deserialize<ChatStreamMediaEventResponse>(receivedText);
                data = mediaResponse?.Body?.Payload ?? string.Empty;
                break;
            case "disconnect":
                break;
        }

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

        return (response.Event, data);
    }
}
