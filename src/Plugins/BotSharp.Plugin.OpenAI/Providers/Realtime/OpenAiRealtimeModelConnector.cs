using BotSharp.Abstraction.Realtime;
using BotSharp.Plugin.OpenAI.Models.Realtime;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Task = System.Threading.Tasks.Task;
namespace BotSharp.Plugin.Twilio.Services.Stream;

public class OpenAiRealtimeModelConnector : IRealtimeModelConnector
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private ClientWebSocket _webSocket;

    public OpenAiRealtimeModelConnector(IServiceProvider services, ILogger<OpenAiRealtimeModelConnector> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task Connect(Action<string> onAudioDeltaReceived, Action onAudioResponseDone, Action onUserInterrupted)
    {
        var model = "gpt-4o-mini-realtime-preview-2024-12-17";
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(provider: "openai", model);

        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {settings.ApiKey}");
        _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

        await _webSocket.ConnectAsync(new Uri($"wss://api.openai.com/v1/realtime?model={model}"), CancellationToken.None);

        if (_webSocket.State == WebSocketState.Open)
        {
            // Receive a message
            ReceiveMessage(onAudioDeltaReceived, onAudioResponseDone, onUserInterrupted);

            // Control initial session with OpenAI
            var sessionUpdate = new
            {
                type = "session.update",
                session = new
                {
                    turn_detection = new { type = "server_vad" },
                    input_audio_format = "g711_ulaw",
                    output_audio_format = "g711_ulaw",
                    voice = "alloy",
                    instructions = "You are a helpful and bubbly AI assistant who loves to chat about anything the user is interested about and is prepared to offer them facts. You have a penchant for dad jokes, owl jokes, and rickrolling – subtly. Always stay positive, but work in a joke when appropriate.",
                    modalities = new string[] { "text", "audio" },
                    temperature = 0.8f,
                }
            };

            await SendEventToWebSocket(sessionUpdate);

            var initialConversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new object[]
                    {
                    new {
                        type = "input_text",
                        text = "Greet the user with \"Hello there! I am an AI voice assistant powered by Twilio and the OpenAI Realtime API. You can ask me for facts, jokes, or anything you can imagine. How can I help you?\""
                    }
                    }
                }
            };

            await SendEventToWebSocket(initialConversationItem);

            await SendEventToWebSocket(new { type = "response.create" });
        }
    }

    public async Task Disconnect()
    {
        await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
    }

    public async Task SendMessage(string message)
    {
        var audioAppend = new
        {
            type = "input_audio_buffer.append",
            audio = message
        };

        await SendEventToWebSocket(audioAppend);
    }

    private async Task ReceiveMessage(Action<string> onAudioDeltaReceived, Action onAudioResponseDone, Action onUserInterrupted)
    {
        var buffer = new byte[1024 * 1024 * 1];
        WebSocketReceiveResult result;
        string lastAssistantItem = "";
        do
        {
            result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            // Convert received data to text/audio (Twilio sends Base64-encoded audio)
            string receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }
            _logger.LogDebug($"{nameof(OpenAiRealtimeModelConnector)} received: {receivedText}");
            var response = JsonSerializer.Deserialize<ServerEventResponse>(receivedText);
            if (response.Type == "session.created")
            {

            }
            else if (response.Type == "session.updated")
            {

            }
            else if (response.Type == "response.audio_transcript.delta")
            {

            }
            else if (response.Type == "response.audio_transcript.done")
            {

            }
            else if (response.Type == "response.audio.delta")
            {
                var audio = JsonSerializer.Deserialize<ResponseAudioDelta>(receivedText);
                lastAssistantItem = audio?.ItemId ?? "";

                if (audio != null && audio.Delta != null)
                {
                    onAudioDeltaReceived(audio.Delta);
                }
            }
            else if (response.Type == "response.audio.done")
            {
                onAudioResponseDone();
            }
            else if (response.Type == "response.done")
            {

            }
            else if (response.Type == "input_audio_buffer.speech_started")
            {
                // var elapsedTime = latestMediaTimestamp - responseStartTimestampTwilio;
                // handle use interuption
                var truncateEvent = new
                {
                    type = "conversation.item.truncate",
                    item_id = lastAssistantItem,
                    content_index = 0,
                    audio_end_ms = 100
                };

                await SendEventToWebSocket(truncateEvent);
                onUserInterrupted();
            }

        } while (!result.CloseStatus.HasValue);

        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task SendEventToWebSocket(object message)
    {
        var data = JsonSerializer.Serialize(message);

        var buffer = Encoding.UTF8.GetBytes(data);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}