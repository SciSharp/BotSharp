using BotSharp.Abstraction.Realtime;
using BotSharp.Plugin.Twilio.Models.Stream;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.WebSockets;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services.Stream;

/// <summary>
/// Refrence to https://github.com/twilio-samples/speech-assistant-openai-realtime-api-node/blob/main/index.js
/// </summary>
public class TwilioStreamMiddleware
{
    private readonly RequestDelegate _next;

    public TwilioStreamMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var request = httpContext.Request;

        if (request.Path.StartsWithSegments("/twilio/stream"))
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var services = httpContext.RequestServices;
                using WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocket(services, webSocket);
            }
        }

        await _next(httpContext);
    }

    private async Task HandleWebSocket(IServiceProvider services, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result;
        var twilioHub = services.GetRequiredService<TwilioStreamHub>();
        var modelConnector = services.GetRequiredService<IRealtimeModelConnector>();
        var logger = services.GetRequiredService<ILogger<TwilioStreamMiddleware>>();

        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            // Convert received data to text/audio (Twilio sends Base64-encoded audio)
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            logger.LogDebug($"{nameof(TwilioStreamMiddleware)} received: {receivedText}");
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }
            var response = JsonSerializer.Deserialize<StreamEventResponse>(receivedText);
            if (response.Event == "start")
            {
                var startResponse = JsonSerializer.Deserialize<StreamEventStartResponse>(receivedText);
                var hubConnectionContext = new HubConnectionContext(new DefaultConnectionContext(startResponse.StreamSid),
                    new HubConnectionContextOptions(),
                    NullLoggerFactory.Instance);
                twilioHub.Context = new TwilioHubCallerContext(hubConnectionContext);

                await twilioHub.OnConnectedAsync();
                await modelConnector.Connect(onAudioDeltaReceived: async audioDeltaData =>
                {
                    var raudioDelta = new
                    {
                        @event = "media",
                        streamSid = startResponse.StreamSid,
                        media = new { payload = audioDeltaData }
                    };

                    await SendEventToWebSocket(webSocket, raudioDelta);
                }, onAudioResponseDone: async () =>
                {
                    var mark = new
                    {
                        @event = "mark",
                        streamSid = startResponse.StreamSid,
                        mark = new { name = "responsePart" }
                    };

                    await SendEventToWebSocket(webSocket, mark);
                }, onUserInterrupted: async () =>
                {
                    var mark = new
                    {
                        @event = "clear",
                        streamSid = startResponse.StreamSid
                    };

                    await SendEventToWebSocket(webSocket, mark);
                });
            }
            else if (response.Event == "media")
            {
                var mediaResponse = JsonSerializer.Deserialize<StreamEventMediaResponse>(receivedText);
                var hubConnectionContext = new HubConnectionContext(new DefaultConnectionContext(mediaResponse.StreamSid),
                    new HubConnectionContextOptions(),
                    NullLoggerFactory.Instance);
                twilioHub.Context = new TwilioHubCallerContext(hubConnectionContext);

                await twilioHub.OnMessageReceived(mediaResponse);
                await modelConnector.SendMessage(mediaResponse.Body.Payload);
            }
            else if (response.Event == "mark")
            {

            }
            else if (response.Event == "stop")
            {
                var stopResponse = JsonSerializer.Deserialize<StreamEventStopResponse>(receivedText);
                var hubConnectionContext = new HubConnectionContext(new DefaultConnectionContext(stopResponse.StreamSid),
                    new HubConnectionContextOptions(),
                    NullLoggerFactory.Instance);
                twilioHub.Context = new TwilioHubCallerContext(hubConnectionContext);

                await twilioHub.OnDisconnectedAsync(new WebSocketException("stopped"));
                await modelConnector.Disconnect();
            }

        } while (!result.CloseStatus.HasValue);

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task SendEventToWebSocket(WebSocket webSocket, object message)
    {
        var data = JsonSerializer.Serialize(message);

        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
