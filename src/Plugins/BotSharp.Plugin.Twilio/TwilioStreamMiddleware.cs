using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models.Stream;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
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
                var conversationId = request.Path.Value.Split("/").Last();
                using WebSocket webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                try
                {
                    await HandleWebSocket(services, conversationId, webSocket);
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

    private async Task HandleWebSocket(IServiceProvider services, string conversationId, WebSocket webSocket)
    {
        var hub = services.GetRequiredService<IRealtimeHub>();
        var conn = hub.SetHubConnection(conversationId);
        var completer = hub.SetCompleter("openai");
        
        // load conversation and state
        var convService = services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, []);
        var hooks = services.GetServices<ITwilioSessionHook>();
        foreach (var hook in hooks)
        {
            await hook.OnStreamingStarted(conn);
        }
        convService.States.Save();

        await hub.Listen(webSocket, (receivedText) =>
        {
            var response = JsonSerializer.Deserialize<StreamEventResponse>(receivedText);
            conn.StreamId = response.StreamSid;

            switch (response.Event)
            {
                case "start":
                    conn.Event = "user_connected";
                    var startResponse = JsonSerializer.Deserialize<StreamEventStartResponse>(receivedText);
                    conn.Data = JsonSerializer.Serialize(startResponse.Body.CustomParameters);
                    conn.ResetStreamState();
                    break;
                case "media":
                    conn.Event = "user_data_received";
                    var mediaResponse = JsonSerializer.Deserialize<StreamEventMediaResponse>(receivedText);
                    conn.LatestMediaTimestamp = long.Parse(mediaResponse.Body.Timestamp);
                    conn.Data = mediaResponse.Body.Payload;
                    break;
                case "stop":
                    conn.Event = "user_disconnected";
                    break;
                case "mark":
                    conn.Event = "mark";
                    if (conn.MarkQueue.Count > 0) conn.MarkQueue.TryDequeue(out var _);
                    break;
                case "dtmf":
                    var dtmfResponse = JsonSerializer.Deserialize<StreamEventDtmfResponse>(receivedText);
                    if (dtmfResponse.Body.Digit == "#")
                    {
                        conn.Event = "user_dtmf_received";
                        conn.Data = conn.KeypadInputBuffer;
                        conn.KeypadInputBuffer = string.Empty;
                    }
                    else
                    {
                        conn.KeypadInputBuffer += dtmfResponse.Body.Digit;
                    }
                    break;
                default:
                    conn.Event = response.Event;
                    break;
            }

            conn.OnModelMessageReceived = message =>
                new
                {
                    @event = "media",
                    streamSid = response.StreamSid,
                    media = new { payload = message }
                };
            conn.OnModelAudioResponseDone = () =>
                new
                {
                    @event = "mark",
                    streamSid = response.StreamSid,
                    mark = new { name = "responsePart" }
                };
            conn.OnModelUserInterrupted = () =>
                new
                {
                    @event = "clear",
                    streamSid = response.StreamSid
                };
        });
    }
}
