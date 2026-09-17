using BotSharp.Abstraction.Realtime.Settings;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

public partial class LiveCompletionProvider
{
    private readonly StringBuilder _outputTranscript = new();
    private readonly StringBuilder _inputTranscript = new();
    private readonly object _transcriptLock = new();

    private IdleFlushTimer? _outputTranscriptTimer;
    private IdleFlushTimer? _inputTranscriptTimer;
    private IdleFlushTimer? _audioIdleTimer;

    private LiveResponseUsage? _lastBackendUsage;

    /// <summary>
    /// Everything that touches the conversation - running a tool, recording a turn - runs here
    /// rather than on the receive loop, which has to stay free to drain audio. See
    /// <see cref="SerialWorkQueue"/> for why it is serial.
    /// </summary>
    private SerialWorkQueue? _conversationWork;

    /// <summary>
    /// Who spoke most recently, so the final flush can emit the turns in the order they happened.
    /// </summary>
    private volatile string? _lastTranscriptRole;

    #region Receive loop
    private async Task ReceiveMessage(RealtimeModelSettings realtimeSettings)
    {
        var session = _session;
        if (session == null) return;

        // Captured rather than read from the fields on the way out. A reconnect replaces both
        // while this loop is unwinding, and tearing down whatever the fields point at by then
        // would close the session that just replaced this one.
        var work = _conversationWork;

        await foreach (ChatSessionUpdate update in session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            LiveServerEvent? response;
            try
            {
                response = JsonSerializer.Deserialize<LiveServerEvent>(receivedText);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Unable to parse {Provider} event: {Payload}", Provider, receivedText);
                continue;
            }

            if (response == null || string.IsNullOrEmpty(response.Type))
            {
                continue;
            }

            if (await HandleServerEvent(response.Type, receivedText))
            {
                break;
            }
        }

        // The stream is over, so whatever is still buffered is a finished turn.
        await FlushPendingTurns(work);

        if (ReferenceEquals(_session, session))
        {
            DisposeSessionWorkers();
        }

        session.Dispose();
    }

    /// <summary>
    /// Returns true when the receive loop should stop.
    /// </summary>
    private async Task<bool> HandleServerEvent(string type, string receivedText)
    {
        switch (type)
        {
            case LiveServerEventType.Error:
                var error = JsonSerializer.Deserialize<LiveErrorEvent>(receivedText);
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                // Command level failures leave the session usable; only transport errors end it.
                return error?.Body?.Type == "server_error";

            case LiveServerEventType.SessionStarted:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                _isBlocking = false;
                await _onModelReady();

                // After _onModelReady, so the backend handler is configured before the model
                // opens its mouth and the greeting cannot arrive ahead of the session setup.
                await SendGreeting();
                return false;

            case LiveServerEventType.SessionUpdated:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                return false;

            case LiveServerEventType.SessionClosed:
                var closed = JsonSerializer.Deserialize<LiveSessionClosedEvent>(receivedText);
                _logger.LogCritical("{Type}: reason {Reason}, billed {Seconds}s",
                    type, closed?.Reason, closed?.Usage?.Seconds);
                // Deliberately not flushed. A turn cut short by the session ending is not a
                // completed turn, and its partial text has already been shown live.
                return true;

            case LiveServerEventType.UsageUpdated:
                var usage = JsonSerializer.Deserialize<LiveUsageUpdatedEvent>(receivedText);
                _logger.LogCritical("{Type}: {Seconds}s, context {Ratio}",
                    type, usage?.Usage?.Seconds, usage?.Usage?.ContextWindow?.UsageRatio);
                return false;

            case LiveServerEventType.OutputAudioDelta:
                await OnOutputAudioDelta(receivedText);
                return false;

            case LiveServerEventType.OutputTranscriptDelta:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                await OnTranscriptDelta(receivedText, _outputTranscript, _outputTranscriptTimer, AgentRole.Assistant);
                return false;

            case LiveServerEventType.InputTranscriptDelta:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                await OnTranscriptDelta(receivedText, _inputTranscript, _inputTranscriptTimer, AgentRole.User);
                return false;

            case LiveServerEventType.ResponseEvent:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                OnBackendResponseEvent(receivedText);
                return false;

            case LiveServerEventType.DelegationCreated:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                await OnDelegationCreated(receivedText);
                return false;

            default:
                _logger.LogCritical("{Type}: {Payload}", type, receivedText);
                return false;
        }
    }
    #endregion

    #region Audio
    private async Task OnOutputAudioDelta(string receivedText)
    {
        var audio = JsonSerializer.Deserialize<LiveOutputAudioDelta>(receivedText);
        if (string.IsNullOrEmpty(audio?.Delta))
        {
            return;
        }

        await _onModelAudioDeltaReceived(audio.Delta, audio.ItemId ?? string.Empty);

        // Audio going quiet is the end of the model's turn, and OnModelAudioIdle closes it.
        // Audio deliberately does not arm the transcript flush any more: on a full duplex session
        // that holds the line open with a continuous stream, that kept the turn open forever.
        _audioIdleTimer?.Touch();
    }
    #endregion

    /// <summary>
    /// The model's audio stream has gone quiet.
    ///
    /// That is the truest end-of-turn signal available - it says the model stopped speaking,
    /// rather than inferring it from a gap between transcript deltas - so the turn is closed here.
    /// Whether it ever runs is the open question: a full duplex connection that holds the line
    /// open with a continuous stream would never go idle, and the turn would then be closed by
    /// the transcript timer or by the user speaking. Logged so it is clear which one did it.
    /// </summary>
    private async Task OnModelAudioIdle()
    {
        _logger.LogInformation("{Provider} model audio idle for {Ms}ms; closing the turn.",
            Provider, LiveSettings.AudioIdleMs);

        await _onModelAudioResponseDone();
        FlushOutputTranscript();
    }

    #region Transcripts
    private async Task OnTranscriptDelta(string receivedText, StringBuilder buffer, IdleFlushTimer? timer, string role)
    {
        var data = JsonSerializer.Deserialize<LiveTranscriptDelta>(receivedText);
        if (string.IsNullOrEmpty(data?.Delta))
        {
            return;
        }

        // Live marks no end of turn, but the speakers take it in turns: the other side starting
        // to talk is what closes the one before it. Flushing here rather than on a timer means a
        // turn is written the moment it is actually over, and a thinking pause never splits it.
        //
        // Both flushes no-op on an empty buffer, so a run of deltas from the same speaker costs
        // nothing. The flush runs before the append so the turns come out in the order spoken.
        if (role == AgentRole.Assistant)
        {
            FlushInputTranscript();
        }
        else
        {
            FlushOutputTranscript();
        }

        lock (_transcriptLock)
        {
            buffer.Append(data.Delta);
        }

        _lastTranscriptRole = role;

        timer?.Touch();

        // Show the words as they are spoken. Storage still waits for the turn to complete, so a
        // half-spoken utterance never becomes a conversation record.
        await PublishTranscriptDelta(role, data.Delta);
    }

    /// <summary>
    /// Pushes a partial transcript straight to the user stream, bypassing the conversation hooks
    /// that write history. Awaited rather than fired off, so deltas arrive in the order spoken.
    /// </summary>
    private async Task PublishTranscriptDelta(string role, string delta)
    {
        var serialize = _conn.OnModelTranscriptDelta;
        var send = _conn.SendEventToUser;

        if (serialize == null || send == null)
        {
            return;
        }

        try
        {
            await send(serialize(role, delta));
        }
        catch (Exception ex)
        {
            // A dead UI socket must not take the voice session down with it.
            _logger.LogError(ex, "Failed to stream a {Role} transcript delta to the user.", role);
        }
    }

    private string TakeBuffer(StringBuilder buffer)
    {
        lock (_transcriptLock)
        {
            var text = buffer.ToString().Trim();
            buffer.Clear();
            return text;
        }
    }

    /// <summary>
    /// Closes the model's turn. The buffer is taken here, on the caller's thread, so the turn
    /// boundary lands where the caller decided it should; recording it is queued, because that
    /// reaches conversation storage and hooks and must not hold up the receive loop.
    /// </summary>
    private void FlushOutputTranscript()
    {
        _outputTranscriptTimer?.Cancel();

        var text = TakeBuffer(_outputTranscript);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _logger.LogInformation("{Provider} model transcript: {Transcript}", Provider, text);

        // Read now rather than inside the queued work: by the time that runs the model may
        // already be speaking its next turn, and this turn would be filed under its item id.
        var messageId = _conn.LastAssistantItemId ?? Guid.NewGuid().ToString();

        _conversationWork?.Enqueue(() => DeliverOutputTranscript(text, messageId));
    }

    private async Task DeliverOutputTranscript(string text, string messageId)
    {
        await _onModelAudioTranscriptDone(text);

        var message = new RoleDialogModel(AgentRole.Assistant, text)
        {
            CurrentAgentId = _conn.CurrentAgentId,
            MessageId = messageId,
            MessageType = MessageTypeName.Plain
        };

        // Deliver before telemetry. The buffer is already drained by this point, so anything that
        // throws on the way out loses the turn for good - and the queue only logs what throws,
        // so the message would vanish without a trace.
        await _onModelResponseDone([message]);

        try
        {
            await ReportGenerated(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report {Provider} token stats.", Provider);
        }
    }

    /// <summary>
    /// Writes out whatever is still buffered when the conversation ends.
    ///
    /// Alternation closes every turn but the last one: nobody speaks after it, so nothing
    /// triggers its flush. Once the stream is over no further delta can arrive, which makes the
    /// buffer a complete turn rather than a half-finished one - so this records it instead of
    /// discarding it. Safe to call more than once; both flushes no-op on an empty buffer.
    ///
    /// Queues the turns like any other flush and then drains, so the last words of the call
    /// reach storage before the session is torn down.
    /// </summary>
    /// <param name="work">
    /// The queue to drain, for a caller that may no longer own the one in the field.
    /// </param>
    private async Task FlushPendingTurns(SerialWorkQueue? work = null)
    {
        // Emit in the order they were spoken, so whoever spoke last is written last. Each is
        // guarded separately: this runs during teardown, where one failing turn must neither
        // take the other down with it nor break the caller's shutdown path.
        var flushes = _lastTranscriptRole == AgentRole.Assistant
            ? new Action[] { FlushInputTranscript, FlushOutputTranscript }
            : [FlushOutputTranscript, FlushInputTranscript];

        foreach (var flush in flushes)
        {
            try
            {
                flush();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush the final {Provider} transcript.", Provider);
            }
        }

        var queue = work ?? _conversationWork;
        if (queue != null)
        {
            await queue.DrainAsync();
        }
    }

    /// <summary>
    /// Closes the caller's turn. Split the same way as <see cref="FlushOutputTranscript"/>:
    /// the boundary is taken here, the recording is queued.
    /// </summary>
    private void FlushInputTranscript()
    {
        _inputTranscriptTimer?.Cancel();

        var text = TakeBuffer(_inputTranscript);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _logger.LogInformation("{Provider} user transcript: {Transcript}", Provider, text);

        _conversationWork?.Enqueue(() => _onInputAudioTranscriptionDone(new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = _conn.CurrentAgentId
        }));
    }

    /// <summary>
    /// Live voice time is billed per second rather than per token; token counts come from
    /// the backend handler and are zero until it has produced a response.
    /// </summary>
    private async Task ReportGenerated(string text)
    {
        var usage = _lastBackendUsage;
        _lastBackendUsage = null;

        var contentHooks = _services.GetHooks<IContentGeneratingHook>(_conn.CurrentAgentId);
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = _conn.CurrentAgentId
            },
            new TokenStatsModel
            {
                Provider = Provider,
                Model = _model,
                Prompt = text,
                TextInputTokens = (usage?.InputTokens ?? 0) - (usage?.InputTokenDetails?.CachedTokens ?? 0),
                CachedTextInputTokens = usage?.InputTokenDetails?.CachedTokens ?? 0,
                TextOutputTokens = usage?.OutputTokens ?? 0
            });
        }
    }
    #endregion

    #region Delegation
    /// <summary>
    /// Unwraps a backend Responses event. Completed tool calls are surfaced to the hub so
    /// the existing routing pipeline executes them.
    /// </summary>
    private void OnBackendResponseEvent(string receivedText)
    {
        var envelope = JsonSerializer.Deserialize<LiveResponseEventEnvelope>(receivedText);
        var inner = envelope?.Event;
        if (inner == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(envelope?.DelegationId))
        {
            _currentDelegationId = envelope.DelegationId;
        }

        switch (inner.Type)
        {
            case LiveResponseInnerEventType.OutputItemDone:
                OnBackendOutputItemDone(inner);
                return;

            case LiveResponseInnerEventType.Completed:
            case LiveResponseInnerEventType.Incomplete:
            case LiveResponseInnerEventType.Failed:
                _lastBackendUsage = inner.Response?.Usage;
                _logger.LogInformation("{Type}: backend response {Status}", inner.Type, inner.Response?.Status);
                return;

            default:
                _logger.LogDebug("{Type}: {Payload}", inner.Type, receivedText);
                return;
        }
    }

    /// <summary>
    /// A finished item from the backend turn. Two shapes matter: a function_call, which goes to
    /// the existing routing pipeline, and a message, the answer the backend settled on, which is
    /// only logged - the assistant turn is still recorded from the spoken transcript.
    /// </summary>
    private void OnBackendOutputItemDone(LiveInnerResponseEvent inner)
    {
        var item = inner.ResolveItem();
        if (item == null || !item.IsCompleted)
        {
            return;
        }

        var messageId = item.Id ?? inner.ItemId ?? Guid.NewGuid().ToString();

        switch (item.Type)
        {
            case LiveResponseItemType.FunctionCall:
                if (string.IsNullOrEmpty(item.Name))
                {
                    return;
                }

                _logger.LogCritical("{Provider} tool call {Name}({Arguments})", Provider, item.Name, item.Arguments);

                var call = new RoleDialogModel(AgentRole.Assistant, item.Arguments ?? "{}")
                {
                    CurrentAgentId = _conn.CurrentAgentId,
                    FunctionName = item.Name,
                    FunctionArgs = item.Arguments,
                    ToolCallId = item.CallId,
                    MessageId = messageId,
                    MessageType = MessageTypeName.FunctionCall
                };

                // Queued, never awaited here. Running the function inline would stop the socket
                // being read for as long as it takes - and on a full duplex call the model is
                // still speaking through that, so the caller hears the reply cut in half.
                _conversationWork?.Enqueue(() => _onModelResponseDone([call]));
                return;

            case LiveResponseItemType.Message:
                // Logged, not recorded. The conversation still takes its assistant turn from the
                // spoken transcript, and this is the answer the voice model is about to speak,
                // so storing it here would put the same turn in the history twice.
                var text = item.GetOutputText();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                _logger.LogCritical("{Provider} backend {Phase} answer: {Text}",
                    Provider, item.Phase ?? LiveResponsePhase.FinalAnswer, text);
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Raised only under client delegation: the model has handed a task to this application.
    /// Results are returned with session.thinking.append or session.commentary.append.
    /// </summary>
    private async Task OnDelegationCreated(string receivedText)
    {
        var data = JsonSerializer.Deserialize<LiveDelegationCreatedEvent>(receivedText);
        var delegationId = data?.Delegation?.Id;
        _currentDelegationId = delegationId;

        _logger.LogInformation("{Type}: delegation {Id} to {Target}",
            LiveServerEventType.DelegationCreated, delegationId, data?.Delegation?.Target);

        await _onConversationItemCreated(receivedText);
    }
    #endregion

    #region Turn boundary timers and background work
    private void ResetSessionState()
    {
        DisposeSessionWorkers();

        lock (_transcriptLock)
        {
            _outputTranscript.Clear();
            _inputTranscript.Clear();
        }

        _lastBackendUsage = null;
        _lastTranscriptRole = null;

        _conversationWork = new SerialWorkQueue("conversation", _logger);

        var settings = LiveSettings;
        _outputTranscriptTimer = new IdleFlushTimer(
            "model transcript", TimeSpan.FromMilliseconds(settings.TranscriptIdleMs), FlushOutputTranscriptAsync, _logger);
        // The user's buffer has nothing keeping it armed the way model audio arms the other one,
        // so it waits longer: a speaker pausing mid-sentence must not be mistaken for a finished
        // turn now that the model replying is what normally closes it.
        _inputTranscriptTimer = new IdleFlushTimer(
            "user transcript", TimeSpan.FromMilliseconds(settings.InputTranscriptIdleMs), FlushInputTranscriptAsync, _logger);
        _audioIdleTimer = new IdleFlushTimer(
            "model audio", TimeSpan.FromMilliseconds(settings.AudioIdleMs), OnModelAudioIdle, _logger);
    }

    // The flushes only take a buffer and queue its delivery, so there is nothing left to await;
    // the timer still wants a Func<Task>.
    private Task FlushOutputTranscriptAsync()
    {
        FlushOutputTranscript();
        return Task.CompletedTask;
    }

    private Task FlushInputTranscriptAsync()
    {
        FlushInputTranscript();
        return Task.CompletedTask;
    }

    private void DisposeSessionWorkers()
    {
        _outputTranscriptTimer?.Dispose();
        _inputTranscriptTimer?.Dispose();
        _audioIdleTimer?.Dispose();

        _outputTranscriptTimer = null;
        _inputTranscriptTimer = null;
        _audioIdleTimer = null;

        // Already drained by FlushPendingTurns on every path that gets here; this only closes
        // the queue so a late event cannot start work on a session that is gone.
        _conversationWork?.Dispose();
        _conversationWork = null;
    }
    #endregion
}
