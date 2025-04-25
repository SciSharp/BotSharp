using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Models;
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
                    var segments = request.Path.Value.Split("/");
                    var agentId = segments[segments.Length - 2];
                    var conversationId = segments[segments.Length - 1];

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
        var hub = services.GetRequiredService<IRealtimeHub>();
        var conn = hub.SetHubConnection(conversationId);
        conn.CurrentAgentId = agentId;

        // load conversation and state
        var convService = services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, []);
        await convService.GetConversationRecordOrCreateNew(agentId);

        var buffer = new byte[1024 * 32];
        WebSocketReceiveResult result;

        do
        {
            result = await webSocket.ReceiveAsync(new(buffer), CancellationToken.None);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            var receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
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
            }
        }
        while (!webSocket.CloseStatus.HasValue);

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ConnectToModel(IRealtimeHub hub, WebSocket webSocket)
    {
        await hub.ConnectToModel(async data =>
        {
            await SendEventToUser(webSocket, data);
        });
    }

    private async Task SendEventToUser(WebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        if (!webSocket.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
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
                data = mediaResponse?.Payload ?? string.Empty;
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
