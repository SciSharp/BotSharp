using BotSharp.Abstraction.Realtime;
using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.Models.Stream;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services.Stream;

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
                await HandleWebSocket(services, conversationId, webSocket);
                return;
            }
        }

        await _next(httpContext);
    }

    private async Task HandleWebSocket(IServiceProvider services, string conversationId, WebSocket webSocket)
    {
        var hub = services.GetRequiredService<IRealtimeHub>();
        var convService = services.GetRequiredService<IConversationService>();

        // Session state
        var conn = new RealtimeHubConnection
        {
            ConversationId = conversationId
        };

        // Variables for timestamp and interruption handling
        string streamSid = null;
        long latestMediaTimestamp = 0;
        string lastAssistantItem = null;
        var markQueue = new ConcurrentQueue<string>();
        long? responseStartTimestampTwilio = null;

        // Load session and state
        convService.SetConversationId(conversationId, new List<MessageState>());
        var hooks = services.GetServices<ITwilioSessionHook>();
        foreach (var hook in hooks)
        {
            await hook.OnStreamingStarted(conn);
        }
        convService.States.Save();

        // Set up event handlers
        conn.OnModelMessageReceived = message =>
        {
            // Record last assistant item ID for interruption handling
            if (!string.IsNullOrEmpty(conn.StreamId))
            {
                lastAssistantItem = conn.StreamId;
            }

            // If this is the first delta of a new response, set the start timestamp
            if (!responseStartTimestampTwilio.HasValue)
            {
                responseStartTimestampTwilio = latestMediaTimestamp;
                _logger.LogDebug($"Setting start timestamp for new response: {responseStartTimestampTwilio}ms");
            }

            // Add mark to queue
            markQueue.Enqueue("responsePart");

            return new
            {
                @event = "media",
                streamSid = conn.StreamId,
                media = new { payload = message }
            };
        };

        conn.OnModelAudioResponseDone = () =>
        {
            return new
            {
                @event = "mark",
                streamSid = conn.StreamId,
                mark = new { name = "responsePart" }
            };
        };

        conn.OnModelUserInterrupted = () =>
        {
            // Reset states
            markQueue.Clear();
            lastAssistantItem = null;
            responseStartTimestampTwilio = null;

            return new
            {
                @event = "clear",
                streamSid = conn.StreamId
            };
        };

        try
        {
            await hub.Listen(webSocket, receivedText =>
            {
                var response = JsonSerializer.Deserialize<StreamEventResponse>(receivedText);
                if (response == null)
                {
                    _logger.LogWarning("Failed to parse received WebSocket message");
                    return conn;
                }

                conn.StreamId = response.StreamSid;

                switch (response.Event)
                {
                    case "start":
                        conn.Event = "user_connected";
                        streamSid = response.StreamSid;
                        _logger.LogInformation($"Incoming stream started: {streamSid}");

                        // Reset start and media timestamps
                        responseStartTimestampTwilio = null;
                        latestMediaTimestamp = 0;

                        var startResponse = JsonSerializer.Deserialize<StreamEventStartResponse>(receivedText);
                        if (startResponse?.Body?.CustomParameters != null)
                        {
                            conn.Data = JsonSerializer.Serialize(startResponse.Body.CustomParameters);
                        }
                        break;

                    case "media":
                        conn.Event = "user_data_received";
                        var mediaResponse = JsonSerializer.Deserialize<StreamEventMediaResponse>(receivedText);
                        if (mediaResponse?.Body != null)
                        {
                            conn.Data = mediaResponse.Body.Payload;

                            // Update latest media timestamp
                            if (long.TryParse(mediaResponse.Body.Timestamp, out latestMediaTimestamp))
                            {
                                _logger.LogDebug($"Received media message with timestamp: {latestMediaTimestamp}ms");
                            }                            

                            // Check if user started speaking (interruption handling)
                            if (markQueue.Count > 0 && responseStartTimestampTwilio.HasValue &&
                                !string.IsNullOrEmpty(lastAssistantItem))
                            {
                                // Detect voice activity - more complex logic can be added here
                                // e.g., check audio energy levels or use VAD (Voice Activity Detection)

                                // If voice activity detected, handle interruption
                                if (ShouldHandleInterruption(mediaResponse.Body.Payload))
                                {
                                    conn.Event = "user_interrupted";
                                    long elapsedTime = latestMediaTimestamp - responseStartTimestampTwilio.Value;
                                    _logger.LogDebug($"Calculating elapsed time for truncation: {latestMediaTimestamp} - {responseStartTimestampTwilio} = {elapsedTime}ms");
                                }
                            }
                        }
                        break;

                    case "mark":
                        // Handle mark event
                        if (markQueue.TryDequeue(out _))
                        {
                            _logger.LogDebug("Processing mark event, removing one mark from queue");
                        }
                        break;

                    case "stop":
                        conn.Event = "user_disconnected";
                        break;

                    default:
                        _logger.LogInformation($"Received non-media event: {response.Event}");
                        break;
                }

                return conn;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket communication");
        }
    }

    // Simple interruption detection logic - can be extended as needed
    private bool ShouldHandleInterruption(string audioPayload)
    {
        // Here should implement actual voice activity detection logic
        // e.g., analyze audio energy levels or use VAD algorithm
        
        // Simple example - should be replaced with real detection logic in production
        if (!string.IsNullOrEmpty(audioPayload))
        {
            // Check if audio payload contains sufficient energy
            // This is just a placeholder - needs actual VAD implementation
            return false; // Default to false to avoid false interruptions
        }
        
        return false;
    }
}
