using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Models.Relay;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// One phone call on Twilio ConversationRelay: Twilio sends what the caller said as text and speaks the text we send back.
/// https://www.twilio.com/docs/voice/conversationrelay/websocket-messages
/// </summary>
public class ConversationRelaySession
{
    private const string DefaultSayItAgainText = "Sorry, can you say it again?";
    private const string DefaultErrorText = "Sorry, something went wrong. Please call back later.";
    private const double SpokenCharsPerSecond = 15;

    private readonly WebSocket _socket;
    private readonly string _agentId;
    private readonly string _conversationId;
    private readonly TwilioSetting _settings;
    private readonly Func<CallerMessage, Func<string, Task>, Task<AssistantMessage?>> _sendCallerMessage;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _sync = new();
    private readonly List<string> _pendingInputs = [];
    private readonly StringBuilder _digits = new();
    private readonly StringBuilder _partialPrompt = new();
    private string? _pendingDigits;
    private bool _isProcessing;
    private Task _processing = Task.CompletedTask;
    private int _seqNum;
    private int _idlePrompts;
    private bool _ended;

    // Caller silence is measured from this moment: the estimated end of what we last said, or the caller's last input
    private DateTime _quietSinceUtc = DateTime.MaxValue;

    private string? _callSid;
    private string? _from;
    private string? _to;
    private string? _direction;

    /// <param name="sendCallerMessage">Sends the caller's words to the agent and returns its reply; the second argument receives tool-call indications</param>
    public ConversationRelaySession(
        WebSocket socket,
        string agentId,
        string conversationId,
        TwilioSetting settings,
        Func<CallerMessage, Func<string, Task>, Task<AssistantMessage?>> sendCallerMessage,
        ILogger logger)
    {
        _socket = socket;
        _agentId = agentId;
        _conversationId = conversationId;
        _settings = settings;
        _sendCallerMessage = sendCallerMessage;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var idleWatch = WatchIdleAsync(cts.Token);
        try
        {
            await ReceiveLoopAsync(cts.Token);
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            _logger.LogInformation($"ConversationRelay socket closed for conversation {_conversationId}: {ex.Message}");
        }
        finally
        {
            cts.Cancel();
            await SwallowAsync(idleWatch);
        }

        // Let a turn already running finish so the conversation is saved
        await SwallowAsync(_processing);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();
        while (_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (_socket.State == WebSocketState.CloseReceived)
                {
                    await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                break;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var json = Encoding.UTF8.GetString(message.ToArray());
            message.SetLength(0);
            try
            {
                await HandleMessageAsync(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"Unreadable ConversationRelay message for conversation {_conversationId}: {ex.Message}");
            }
        }
    }

    private async Task HandleMessageAsync(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        switch (GetString(root, "type"))
        {
            case "setup":
                _callSid = GetString(root, "callSid");
                _from = GetString(root, "from");
                _to = GetString(root, "to");
                _direction = GetString(root, "direction");
                var greetingLength = 0;
                if (root.TryGetProperty("customParameters", out var parameters)
                    && GetString(parameters, ConversationRelayParameters.WelcomeGreetingLength) is string length)
                {
                    int.TryParse(length, out greetingLength);
                }
                lock (_sync)
                {
                    _quietSinceUtc = DateTime.UtcNow + EstimateSpeech(greetingLength);
                }
                _logger.LogInformation($"ConversationRelay session started for conversation {_conversationId} (call {_callSid}).");
                break;

            case "prompt":
                var text = GetString(root, "voicePrompt") ?? string.Empty;
                var last = !root.TryGetProperty("last", out var lastElement) || lastElement.ValueKind != JsonValueKind.False;
                _partialPrompt.Append(text);
                if (last)
                {
                    var prompt = _partialPrompt.ToString().Trim();
                    _partialPrompt.Clear();
                    if (prompt.Length > 0)
                    {
                        Enqueue(prompt, digits: null);
                    }
                }
                break;

            case "dtmf":
                var digit = GetString(root, "digit");
                if (digit == "#")
                {
                    var digits = _digits.ToString();
                    _digits.Clear();
                    if (digits.Length > 0)
                    {
                        Enqueue(digits, digits);
                    }
                }
                else if (!string.IsNullOrEmpty(digit))
                {
                    _digits.Append(digit);
                    lock (_sync)
                    {
                        _quietSinceUtc = DateTime.UtcNow;
                    }
                }
                break;

            case "interrupt":
                // Playback stopped where the caller cut in; the caller's words follow as a prompt
                lock (_sync)
                {
                    _quietSinceUtc = DateTime.UtcNow;
                }
                _logger.LogInformation($"Caller interrupted the agent in conversation {_conversationId} after {GetString(root, "durationUntilInterruptMs")} ms.");
                break;

            case "error":
                _logger.LogWarning($"ConversationRelay error in conversation {_conversationId}: {GetString(root, "description")}");
                break;
        }

        await Task.CompletedTask;
    }

    private void Enqueue(string content, string? digits)
    {
        lock (_sync)
        {
            if (_ended)
            {
                return;
            }

            _pendingInputs.Add(content);
            if (digits != null)
            {
                _pendingDigits = digits;
            }
            _quietSinceUtc = DateTime.UtcNow;
            _idlePrompts = 0;

            if (!_isProcessing)
            {
                _isProcessing = true;
                _processing = Task.Run(ProcessPendingAsync);
            }
        }
    }

    /// <summary>
    /// Runs one agent turn at a time; what the caller says meanwhile is merged into the next turn.
    /// </summary>
    private async Task ProcessPendingAsync()
    {
        while (true)
        {
            CallerMessage message;
            lock (_sync)
            {
                if (_ended || _pendingInputs.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }

                message = new CallerMessage
                {
                    AgentId = _agentId,
                    ConversationId = _conversationId,
                    SeqNumber = ++_seqNum,
                    Content = string.Join("\r\n", _pendingInputs),
                    Digits = _pendingDigits,
                    From = (string.Equals(_direction, "inbound", StringComparison.OrdinalIgnoreCase) ? _from : _to) ?? string.Empty
                };
                _pendingInputs.Clear();
                _pendingDigits = null;
            }

            AssistantMessage? reply;
            try
            {
                // Run the turn outside the WebSocket request's execution context; it sets up its own HttpContext
                Task<AssistantMessage?> turn;
                using (ExecutionContext.SuppressFlow())
                {
                    turn = Task.Run(() => _sendCallerMessage(message, SpeakIndicationAsync));
                }

                var timeout = Task.Delay(TimeSpan.FromSeconds(_settings.ConversationRelay.AiResponseTimeoutSeconds));
                if (await Task.WhenAny(turn, timeout) != turn)
                {
                    _logger.LogWarning($"Agent did not reply within {_settings.ConversationRelay.AiResponseTimeoutSeconds}s in conversation {_conversationId}.");
                    await FailAsync();
                    return;
                }
                reply = await turn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Agent turn failed in conversation {_conversationId}.");
                await FailAsync();
                return;
            }

            if (reply == null || string.IsNullOrWhiteSpace(reply.Content) && !reply.ConversationEnd && !reply.HumanIntervationNeeded)
            {
                continue;
            }

            await SpeakAsync(reply.Content);
            if (reply.HumanIntervationNeeded)
            {
                await EndAsync(ConversationRelayHandoff.Transfer);
                return;
            }
            if (reply.ConversationEnd)
            {
                await EndAsync(ConversationRelayHandoff.HangUp);
                return;
            }
        }
    }

    private async Task WatchIdleAsync(CancellationToken cancellationToken)
    {
        var relay = _settings.ConversationRelay;
        var idleTimeout = TimeSpan.FromSeconds(relay.IdleTimeoutSeconds);
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

            var askAgain = false;
            var hangUp = false;
            lock (_sync)
            {
                if (_ended || _isProcessing || _quietSinceUtc == DateTime.MaxValue || DateTime.UtcNow - _quietSinceUtc < idleTimeout)
                {
                    continue;
                }

                if (_idlePrompts < relay.MaxIdlePrompts)
                {
                    _idlePrompts++;
                    askAgain = true;
                }
                else
                {
                    hangUp = true;
                }
                _quietSinceUtc = DateTime.UtcNow;
            }

            if (askAgain)
            {
                await SpeakPromptAsync($"say-it-again-{Random.Shared.Next(1, 5)}", DefaultSayItAgainText);
            }
            else if (hangUp)
            {
                _logger.LogInformation($"Caller stayed silent, hanging up conversation {_conversationId}.");
                await EndAsync(ConversationRelayHandoff.HangUp);
            }
        }
    }

    private async Task FailAsync()
    {
        await SpeakPromptAsync("error", DefaultErrorText);
        await EndAsync(ConversationRelayHandoff.HangUp);
    }

    /// <summary>
    /// Speaks a tool-call indication: "#name" is a prerecorded prompt, anything else is spoken as is; segments are separated by "|".
    /// </summary>
    private async Task SpeakIndicationAsync(string indication)
    {
        foreach (var segment in indication.Split('|'))
        {
            var text = segment.Trim();
            if (text.StartsWith('#'))
            {
                await SpeakPromptAsync(text.Substring(1), fallbackText: null);
            }
            else
            {
                await SpeakAsync(text);
            }
        }
    }

    /// <summary>
    /// Speaks a prerecorded prompt's configured text so the voice matches; without one, plays the recording.
    /// </summary>
    private async Task SpeakPromptAsync(string name, string? fallbackText)
    {
        if (_settings.PromptTexts.TryGetValue(name, out var text) && !string.IsNullOrWhiteSpace(text))
        {
            await SpeakAsync(text);
        }
        else if (fallbackText != null)
        {
            await SpeakAsync(fallbackText);
        }
        else
        {
            await SendAsync(new { type = "play", source = $"{_settings.CallbackHost}/twilio/{name}.mp3" });
            MarkSpeaking(TimeSpan.FromSeconds(3));
        }
    }

    private async Task SpeakAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lock (_sync)
        {
            if (_ended)
            {
                return;
            }
        }

        await SendAsync(new { type = "text", token = text, last = true });
        MarkSpeaking(EstimateSpeech(text.Length));
    }

    private async Task EndAsync(string action)
    {
        lock (_sync)
        {
            if (_ended)
            {
                return;
            }
            _ended = true;
        }

        await SendAsync(new { type = "end", handoffData = JsonSerializer.Serialize(new { action }) });
    }

    private async Task SendAsync(object payload)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await _sendLock.WaitAsync();
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
        {
            // The call already moved on, e.g. a hang-up or transfer redirected it
            _logger.LogInformation($"ConversationRelay socket for conversation {_conversationId} is gone: {ex.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private void MarkSpeaking(TimeSpan duration)
    {
        lock (_sync)
        {
            var start = _quietSinceUtc == DateTime.MaxValue || _quietSinceUtc < DateTime.UtcNow ? DateTime.UtcNow : _quietSinceUtc;
            _quietSinceUtc = start + duration;
        }
    }

    private static TimeSpan EstimateSpeech(int characters)
    {
        return TimeSpan.FromSeconds(characters / SpokenCharsPerSecond + 1);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static async Task SwallowAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
