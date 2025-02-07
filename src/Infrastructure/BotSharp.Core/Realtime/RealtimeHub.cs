using BotSharp.Abstraction.Realtime;
using System.Net.WebSockets;
using System;
using BotSharp.Abstraction.Realtime.Models;

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
        var modelConnector = _services.GetRequiredService<IRealtimeModelConnector>();

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
            if (conn.Event == "connected")
            {
                await ConnectToModel(modelConnector, userWebSocket, conn);
            }
            else if (conn.Event == "data_received")
            {
                await modelConnector.SendMessage(conn.Data);
            }
            else if (conn.Event == "disconnected")
            {
                await modelConnector.Disconnect();
            }
        } while (!result.CloseStatus.HasValue);

        await userWebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ConnectToModel(IRealtimeModelConnector modelConnector, WebSocket userWebSocket, RealtimeHubConnection conn)
    {
        await modelConnector.Connect(conn, onAudioDeltaReceived: async audioDeltaData =>
        {
            var data = conn.OnModelMessageReceived(audioDeltaData);
            await SendEventToWebSocket(userWebSocket, data);
        }, 
        onAudioResponseDone: async () =>
        {
            var data = conn.OnModelAudioResponseDone();
            await SendEventToWebSocket(userWebSocket, data);
        }, 
        onUserInterrupted: async () =>
        {
            var data = conn.OnModelUserInterrupted();
            await SendEventToWebSocket(userWebSocket, data);
        });
    }

    private async Task SendEventToWebSocket(WebSocket webSocket, object message)
    {
        var data = JsonSerializer.Serialize(message);

        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
