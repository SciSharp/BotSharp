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
    /// Who spoke most recently, so the final flush can emit the turns in the order they happened.
    /// </summary>
    private volatile string? _lastTranscriptRole;

    #region Receive loop
    private async Task ReceiveMessage(RealtimeModelSettings realtimeSettings)
    {
        if (_session == null) return;

        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
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
        await FlushPendingTurns();

        DisposeTurnTimers();
        _session?.Dispose();
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
                await OnBackendResponseEvent(receivedText);
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
        await FlushOutputTranscript();
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
            await FlushInputTranscript();
        }
        else
        {
            await FlushOutputTranscript();
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

    private async Task FlushOutputTranscript()
    {
        _outputTranscriptTimer?.Cancel();

        var text = TakeBuffer(_outputTranscript);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _logger.LogInformation("{Provider} model transcript: {Transcript}", Provider, text);

        await _onModelAudioTranscriptDone(text);

        var message = new RoleDialogModel(AgentRole.Assistant, text)
        {
            CurrentAgentId = _conn.CurrentAgentId,
            MessageId = _conn.LastAssistantItemId ?? Guid.NewGuid().ToString(),
            MessageType = MessageTypeName.Plain
        };

        // Deliver before telemetry. The buffer is already drained by this point, so anything that
        // throws on the way out loses the turn for good - and when the flush is driven by the
        // idle timer the exception is only logged, so the message vanishes without a trace.
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
    /// </summary>
    private async Task FlushPendingTurns()
    {
        // Emit in the order they were spoken, so whoever spoke last is written last. Each is
        // guarded separately: this runs during teardown, where one failing turn must neither
        // take the other down with it nor break the caller's shutdown path.
        var flushes = _lastTranscriptRole == AgentRole.Assistant
            ? new Func<Task>[] { FlushInputTranscript, FlushOutputTranscript }
            : [FlushOutputTranscript, FlushInputTranscript];

        foreach (var flush in flushes)
        {
            try
            {
                await flush();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush the final {Provider} transcript.", Provider);
            }
        }
    }

    private async Task FlushInputTranscript()
    {
        _inputTranscriptTimer?.Cancel();

        var text = TakeBuffer(_inputTranscript);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _logger.LogInformation("{Provider} user transcript: {Transcript}", Provider, text);

        await _onInputAudioTranscriptionDone(new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = _conn.CurrentAgentId
        });
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
    private async Task OnBackendResponseEvent(string receivedText)
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
                var item = inner.ResolveItem();
                if (item?.Type != "function_call" || string.IsNullOrEmpty(item.Name))
                {
                    return;
                }

                _logger.LogInformation("{Provider} tool call {Name}({Arguments})", Provider, item.Name, item.Arguments);

                await _onModelResponseDone([
                    new RoleDialogModel(AgentRole.Assistant, item.Arguments ?? "{}")
                    {
                        CurrentAgentId = _conn.CurrentAgentId,
                        FunctionName = item.Name,
                        FunctionArgs = item.Arguments,
                        ToolCallId = item.CallId,
                        MessageId = item.Id ?? inner.ItemId ?? Guid.NewGuid().ToString(),
                        MessageType = MessageTypeName.FunctionCall
                    }
                ]);
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

    #region Turn boundary timers
    private void ResetTurnBuffers()
    {
        DisposeTurnTimers();

        lock (_transcriptLock)
        {
            _outputTranscript.Clear();
            _inputTranscript.Clear();
        }

        _lastBackendUsage = null;
        _lastTranscriptRole = null;

        var settings = LiveSettings;
        _outputTranscriptTimer = new IdleFlushTimer(
            "model transcript", TimeSpan.FromMilliseconds(settings.TranscriptIdleMs), FlushOutputTranscript, _logger);
        // The user's buffer has nothing keeping it armed the way model audio arms the other one,
        // so it waits longer: a speaker pausing mid-sentence must not be mistaken for a finished
        // turn now that the model replying is what normally closes it.
        _inputTranscriptTimer = new IdleFlushTimer(
            "user transcript", TimeSpan.FromMilliseconds(settings.InputTranscriptIdleMs), FlushInputTranscript, _logger);
        _audioIdleTimer = new IdleFlushTimer(
            "model audio", TimeSpan.FromMilliseconds(settings.AudioIdleMs), OnModelAudioIdle, _logger);
    }

    private void DisposeTurnTimers()
    {
        _outputTranscriptTimer?.Dispose();
        _inputTranscriptTimer?.Dispose();
        _audioIdleTimer?.Dispose();

        _outputTranscriptTimer = null;
        _inputTranscriptTimer = null;
        _audioIdleTimer = null;
    }
    #endregion
}
