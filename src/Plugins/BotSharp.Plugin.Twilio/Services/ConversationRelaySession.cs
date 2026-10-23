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
    private const string DefaultHoldOnText = "One moment, please.";
    private const double SpokenCharsPerSecond = 15;
    private static readonly List<string> DefaultWatchdogPrompts = ["hold-on-1", "hold-on-2", "hold-on-3", "hold-on-4", "hold-on-5"];
    private static readonly List<string> DefaultWatchdogLongPrompts = Enumerable.Range(1, 10).Select(i => $"hold-on-long-{i}").ToList();
    private static readonly TimeSpan WatchdogPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly WebSocket _socket;
    private readonly string _agentId;
    private readonly string _conversationId;
    private readonly TwilioSetting _settings;
    private readonly Func<CallerMessage, AgentTurnCallbacks, Task<AssistantMessage?>> _sendCallerMessage;
    private readonly ILogger _logger;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _sync = new();
    private readonly List<PendingInput> _pendingInputs = [];
    private readonly StringBuilder _digits = new();
    private readonly StringBuilder _partialPrompt = new();
    private bool _isProcessing;
    private Task _processing = Task.CompletedTask;
    private int _seqNum;
    private int _idlePrompts;
    private bool _ended;

    // Bumped on every caller input, so a decision taken during silence can tell the caller spoke since
    private int _activity;

    // Caller silence is measured from this moment: the estimated end of what we last said, or the caller's last input
    private DateTime _quietSinceUtc = DateTime.MaxValue;

    // Streaming of the current turn's reply (ConversationRelay.StreamReplies)
    private readonly StringBuilder _streamSegment = new();
    private string _lastStreamSegment = string.Empty;
    private bool _utteranceOpen;
    private bool _streamedThisTurn;
    private bool _dropStream;

    // The current turn's reply is in; the response watchdog says nothing more once it is set
    private bool _turnReplied;
    private DateTime _turnStartedUtc;

    private string? _callSid;
    private string? _from;
    private string? _to;
    private string? _direction;

    /// <param name="sendCallerMessage">
    /// Sends the caller's words to the agent and returns its reply; the callbacks receive tool-call indications
    /// and the streamed reply
    /// </param>
    public ConversationRelaySession(
        WebSocket socket,
        string agentId,
        string conversationId,
        TwilioSetting settings,
        Func<CallerMessage, AgentTurnCallbacks, Task<AssistantMessage?>> sendCallerMessage,
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

        // The call is gone: start no new turns, but let a turn already running finish so the conversation is saved
        int dropped;
        lock (_sync)
        {
            _ended = true;
            dropped = _pendingInputs.Count;
            _pendingInputs.Clear();
        }
        if (dropped > 0)
        {
            _logger.LogInformation($"Dropped {dropped} caller input(s) received after the agent's last turn started; " +
                $"conversation {_conversationId} has ended.");
        }
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
                _logger.LogInformation(
                    $"ConversationRelay session started for conversation {_conversationId} (call {_callSid}).");
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
                        _activity++;
                    }
                }
                break;

            case "interrupt":
                // Playback stopped where the caller cut in; the caller's words follow as a prompt
                lock (_sync)
                {
                    _quietSinceUtc = DateTime.UtcNow;

                    // The caller talked over a reply still being streamed: don't send the rest of it
                    if (_isProcessing && _streamedThisTurn)
                    {
                        _dropStream = true;
                    }
                }
                var interruptedAfter = GetString(root, "durationUntilInterruptMs");
                _logger.LogInformation(
                    $"Caller interrupted the agent in conversation {_conversationId} after {interruptedAfter} ms.");
                break;

            case "error":
                _logger.LogWarning(
                    $"ConversationRelay error in conversation {_conversationId}: {GetString(root, "description")}");
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

            _pendingInputs.Add(new PendingInput(content, digits));
            _quietSinceUtc = DateTime.UtcNow;
            _idlePrompts = 0;
            _activity++;

            if (!_isProcessing)
            {
                _isProcessing = true;
                _processing = Task.Run(ProcessPendingAsync);
            }
        }
    }

    /// <summary>
    /// Runs one agent turn at a time; what the caller says meanwhile is merged into the next turn.
    /// A keypad entry ends the merge, so each entry keeps its own Digits.
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

                var batch = new List<PendingInput>();
                foreach (var input in _pendingInputs)
                {
                    batch.Add(input);
                    if (input.Digits != null)
                    {
                        break;
                    }
                }
                _pendingInputs.RemoveRange(0, batch.Count);

                var inbound = string.Equals(_direction, "inbound", StringComparison.OrdinalIgnoreCase);
                message = new CallerMessage
                {
                    AgentId = _agentId,
                    ConversationId = _conversationId,
                    SeqNumber = ++_seqNum,
                    Content = string.Join("\r\n", batch.Select(x => x.Content)),
                    Digits = batch[^1].Digits,
                    From = (inbound ? _from : _to) ?? string.Empty
                };

                _streamSegment.Clear();
                _lastStreamSegment = string.Empty;
                _utteranceOpen = false;
                _streamedThisTurn = false;
                _dropStream = false;
                _turnReplied = false;
                _turnStartedUtc = DateTime.UtcNow;
            }

            using var watchdogCts = new CancellationTokenSource();
            var watchdog = _settings.ConversationRelay.IsResponseWatchdogEnabledFor(_agentId)
                ? WatchResponseAsync(message.SeqNumber, watchdogCts.Token)
                : Task.CompletedTask;

            var stream = _settings.ConversationRelay.StreamReplies;
            var callbacks = new AgentTurnCallbacks
            {
                OnIndication = SpeakIndicationAsync,
                OnStreamText = stream ? StreamTextAsync : null,
                OnStreamStarted = stream ? CloseUtteranceAsync : null,
                // A line said before a slow tool ("Okay, that's everything I need") is spoken now, and the watchdog
                // counts the silence after it instead of waiting for the next response to close it
                OnStreamEndedWithToolCall = stream ? CloseUtteranceAsync : null
            };

            AssistantMessage? reply;
            try
            {
                // Run the turn outside the WebSocket request's execution context; it sets up its own HttpContext
                Task<AssistantMessage?> turn;
                using (ExecutionContext.SuppressFlow())
                {
                    turn = Task.Run(() => _sendCallerMessage(message, callbacks));
                }

                var timeout = Task.Delay(TimeSpan.FromSeconds(_settings.ConversationRelay.AiResponseTimeoutSeconds));
                if (await Task.WhenAny(turn, timeout) != turn)
                {
                    var timeoutSeconds = _settings.ConversationRelay.AiResponseTimeoutSeconds;
                    _logger.LogWarning($"Agent did not reply within {timeoutSeconds}s in conversation {_conversationId}.");
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
            finally
            {
                // The reply is in: a filler that already passed its delay must not slip in ahead of it
                lock (_sync)
                {
                    _turnReplied = true;
                }
                watchdogCts.Cancel();
                await SwallowAsync(watchdog);
            }

            await CloseUtteranceAsync();
            if (reply == null
                || string.IsNullOrWhiteSpace(reply.Content) && !reply.ConversationEnd && !reply.HumanIntervationNeeded)
            {
                continue;
            }

            await SpeakAsync(GetUnstreamedText(reply.Content));
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

    /// <summary>
    /// While the agent works on a reply, says a filler each time the line has been silent for the next
    /// ResponseWatchdogIntervals value, so the caller never sits in a long silence while the model thinks or a tool runs.
    /// Silence counts from the end of whatever was said last (the caller, a filler, an indication), and no filler is
    /// said twice in a turn while unused ones remain.
    /// </summary>
    private async Task WatchResponseAsync(int seqNumber, CancellationToken cancellationToken)
    {
        var relay = _settings.ConversationRelay;
        var said = new HashSet<string>();
        for (var step = 0; ; step++)
        {
            var interval = TimeSpan.FromSeconds(relay.ResponseWatchdogIntervals[Math.Min(step, relay.ResponseWatchdogIntervals.Count - 1)]);
            while (true)
            {
                await Task.Delay(WatchdogPollInterval, cancellationToken);
                lock (_sync)
                {
                    if (_ended || _seqNum != seqNumber || _turnReplied)
                    {
                        return;
                    }
                    // _quietSinceUtc is the estimated end of what was last said, or the caller's last input
                    var quietSince = _quietSinceUtc == DateTime.MaxValue || _quietSinceUtc < _turnStartedUtc ? _turnStartedUtc : _quietSinceUtc;
                    if (!_utteranceOpen && DateTime.UtcNow - quietSince >= interval)
                    {
                        break;
                    }
                }
            }

            var text = PickFillerText(step == 0 ? relay.ResponseWatchdogPrompts : relay.ResponseWatchdogLongPrompts,
                step == 0 ? DefaultWatchdogPrompts : DefaultWatchdogLongPrompts, said);
            if (!await SpeakFillerAsync(seqNumber, text))
            {
                return;
            }
            said.Add(text);
        }
    }

    private async Task<bool> SpeakFillerAsync(int seqNumber, string text)
    {
        await _sendLock.WaitAsync();
        try
        {
            // Decided while holding the send lock, so a reply cannot start streaming between the check and the
            // filler; a stream that already began has set _utteranceOpen and is waiting for this lock
            lock (_sync)
            {
                if (_ended || _seqNum != seqNumber || _turnReplied || _utteranceOpen)
                {
                    return false;
                }
            }
            await SendLockedAsync(new { type = "text", token = text, last = true });
        }
        finally
        {
            _sendLock.Release();
        }
        MarkSpeaking(EstimateSpeech(text.Length));
        return true;
    }

    private string PickFillerText(List<string> names, List<string> defaultNames, HashSet<string> said)
    {
        var texts = (names.Count > 0 ? names : defaultNames)
            .Select(name => _settings.PromptTexts.TryGetValue(name, out var text) ? text : null)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!)
            .Distinct()
            .ToList();
        if (texts.Count == 0)
        {
            return DefaultHoldOnText;
        }

        // A different sentence each time; repeat only once every one has been said
        var unsaid = texts.Where(text => !said.Contains(text)).ToList();
        var pool = unsaid.Count > 0 ? unsaid : texts;
        return pool[Random.Shared.Next(pool.Count)];
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
            var activity = 0;
            lock (_sync)
            {
                var quietFor = DateTime.UtcNow - _quietSinceUtc;
                if (_ended || _isProcessing || _quietSinceUtc == DateTime.MaxValue || quietFor < idleTimeout)
                {
                    continue;
                }

                if (_idlePrompts < relay.MaxIdlePrompts)
                {
                    _idlePrompts++;
                    askAgain = true;
                    activity = _activity;
                }
                else
                {
                    // Decide and end in one step, so caller input arriving now cannot start a turn on a call being hung up
                    _ended = true;
                    hangUp = true;
                }
                _quietSinceUtc = DateTime.UtcNow;
            }

            if (askAgain && IsStillQuiet(activity))
            {
                await SpeakPromptAsync($"say-it-again-{Random.Shared.Next(1, 5)}", DefaultSayItAgainText);
            }
            else if (hangUp)
            {
                _logger.LogInformation($"Caller stayed silent, hanging up conversation {_conversationId}.");
                await SendEndAsync(ConversationRelayHandoff.HangUp);
            }
        }
    }

    private async Task FailAsync()
    {
        // A turn that timed out may still be streaming; keep its words out of the apology
        lock (_sync)
        {
            _dropStream = true;
        }
        await CloseUtteranceAsync();
        await SpeakPromptAsync("error", DefaultErrorText);
        await EndAsync(ConversationRelayHandoff.HangUp);
    }

    /// <summary>
    /// Sends a piece of the reply as the model writes it; Twilio speaks it as it arrives.
    /// </summary>
    private async Task StreamTextAsync(string text)
    {
        lock (_sync)
        {
            if (_ended || _dropStream)
            {
                return;
            }
            _streamSegment.Append(text);
            _utteranceOpen = true;
            _streamedThisTurn = true;
        }

        await SendAsync(new { type = "text", token = text, last = false });
    }

    /// <summary>
    /// Ends the utterance being streamed, so what follows (an indication, the next response) is spoken on its own.
    /// </summary>
    private async Task CloseUtteranceAsync()
    {
        int length;
        lock (_sync)
        {
            if (!_utteranceOpen)
            {
                return;
            }
            _utteranceOpen = false;
            _lastStreamSegment = _streamSegment.ToString();
            _streamSegment.Clear();
            length = _lastStreamSegment.Length;
        }

        await SendAsync(new { type = "text", token = string.Empty, last = true });
        MarkSpeaking(EstimateSpeech(length));
    }

    /// <summary>
    /// The part of the final reply that was not streamed. Normally nothing; the whole reply when it is not
    /// what the model streamed last, e.g. text set by a tool, or a reply rewritten after the model wrote it.
    /// </summary>
    private string GetUnstreamedText(string? reply)
    {
        var final = reply?.Trim() ?? string.Empty;
        string streamed;
        lock (_sync)
        {
            if (!_streamedThisTurn)
            {
                return final;
            }
            if (_dropStream)
            {
                // The caller cut in; their words come next as a new turn
                return string.Empty;
            }
            streamed = _lastStreamSegment.Trim();
        }

        if (final == streamed)
        {
            return string.Empty;
        }
        if (streamed.Length > 0 && final.StartsWith(streamed, StringComparison.Ordinal))
        {
            return final.Substring(streamed.Length);
        }

        _logger.LogInformation(
            $"Reply in conversation {_conversationId} differs from what was streamed; speaking it in full.");
        return final;
    }

    /// <summary>
    /// Speaks a tool-call indication: "#name" is a prerecorded prompt, anything else is spoken as is;
    /// segments are separated by "|".
    /// </summary>
    private async Task SpeakIndicationAsync(string indication)
    {
        await CloseUtteranceAsync();
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

        await SendEndAsync(action);
    }

    /// <summary>
    /// Tells Twilio to end the session; the caller has already marked the session ended.
    /// </summary>
    private async Task SendEndAsync(string action)
    {
        await SendAsync(new { type = "end", handoffData = JsonSerializer.Serialize(new { action }) });
    }

    /// <summary>
    /// True when the caller has not spoken or pressed a key since <paramref name="activity"/> was read
    /// and nothing else took over.
    /// </summary>
    private bool IsStillQuiet(int activity)
    {
        lock (_sync)
        {
            return !_ended && !_isProcessing && _activity == activity;
        }
    }

    private record PendingInput(string Content, string? Digits);

    private async Task SendAsync(object payload)
    {
        await _sendLock.WaitAsync();
        try
        {
            await SendLockedAsync(payload);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Sends with <see cref="_sendLock"/> already held by the caller.
    /// </summary>
    private async Task SendLockedAsync(object payload)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(bytes);
                await _socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
        {
            // The call already moved on, e.g. a hang-up or transfer redirected it
            _logger.LogInformation($"ConversationRelay socket for conversation {_conversationId} is gone: {ex.Message}");
        }
    }

    private void MarkSpeaking(TimeSpan duration)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            var start = _quietSinceUtc == DateTime.MaxValue || _quietSinceUtc < now ? now : _quietSinceUtc;
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
