using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Models.Relay;
using System.IO;
using System.Text.RegularExpressions;
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
    private const string DefaultAskAgainText = "Sorry, I didn't catch that. {0}";
    private const string DefaultStillThereText = "Are you still there?";
    private const string DefaultIdleGoodbyeText = "It sounds like we got disconnected. Please call back anytime.";
    private const string DefaultQuietWaitCheckInText = "I'm still here whenever you're ready.";
    private const double SpokenCharsPerSecond = 15;
    private static readonly List<string> DefaultWatchdogPrompts = ["hold-on-1", "hold-on-2", "hold-on-3", "hold-on-4", "hold-on-5"];
    private static readonly List<string> DefaultWatchdogLongPrompts = Enumerable.Range(1, 10).Select(i => $"hold-on-long-{i}").ToList();
    private static readonly TimeSpan WatchdogPollInterval = TimeSpan.FromMilliseconds(100);

    // Words that come in this soon after a line went out were said before the caller heard any of it: the line starts
    // playing a moment after it is sent, and the transcript of what they said arrives a second or more after they said
    // it. Seen on a dev call: "Sure.", said back to a filler, came in 0.1 s after the address question went out and was
    // taken as the address confirmed.
    private static readonly TimeSpan TooSoonToAnswer = TimeSpan.FromSeconds(1);

    // The fastest a line is spoken: a line played to the end reaches its question no sooner than its words before the
    // question take at this pace, so an answer said once the question was heard is never taken for a word said to what
    // came before it (SpokenCharsPerSecond is the usual pace)
    private const double FastestSpokenCharsPerSecond = 20;

    private readonly WebSocket _socket;
    private readonly string _agentId;
    private readonly string _conversationId;
    private readonly TwilioSetting _settings;
    // The ConversationRelay settings for this agent's calls
    private readonly ConversationRelaySetting _relay;
    private readonly Func<CallerMessage, AgentTurnCallbacks, Task<AssistantMessage?>> _sendCallerMessage;
    private readonly ILogger _logger;
    private readonly Func<string, string, DateTime, Task>? _keepSaidLine;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _sync = new();
    private readonly Queue<PendingInput> _pendingInputs = new();
    private readonly StringBuilder _digits = new();
    private bool _isProcessing;
    private Task _processing = Task.CompletedTask;
    // A turn given up on after AiResponseTimeoutSeconds keeps running until it saves the conversation
    private Task _timedOutTurn = Task.CompletedTask;
    private int _seqNum;
    private int _idlePrompts;
    private bool _ended;

    // Bumped on every caller input, so a decision taken during silence can tell the caller spoke since
    private int _activity;

    // Caller silence is measured from this moment: the estimated end of what we last said, or the caller's last input
    private DateTime _quietSinceUtc = DateTime.MaxValue;

    // Streaming of the current turn's reply (ConversationRelay.StreamReplies)
    private readonly StringBuilder _streamSegment = new();
    // The part of the reply streamed so far that waits for the end of its clause before it is formatted and said
    // (FormatSpeech), and how many characters of what went out were said (the voice's length, not the model's)
    private readonly StringBuilder _speechHeld = new();
    private int _streamSpokenChars;
    private string _lastStreamSegment = string.Empty;
    private bool _utteranceOpen;
    private bool _streamedThisTurn;
    private bool _dropStream;
    private bool _cutInMidReply;
    // What Twilio says it had spoken of the utterance when the caller cut in (utteranceUntilInterrupt)
    private string? _heardBeforeCutIn;

    // The current turn's callbacks, for what the flow wants of the reply as it goes out
    private AgentTurnCallbacks? _turnCallbacks;

    // How the utterance being streamed went out, so its closing frame goes out the same way
    private bool _utteranceKeptUninterrupted;
    // _saidCount, and the time, as its first piece went out
    private int _utteranceStartMark;
    private DateTime _utteranceStartUtc;
    // The caller cut into the utterance being streamed
    private bool _utteranceCutIn;

    // The current turn's reply is in; the response watchdog says nothing more once it is set
    private bool _turnReplied;
    private DateTime _turnStartedUtc;

    // What the current turn has done that the caller would notice, so it can't be taken back once one is set:
    // anything said (a filler, an indication, a streamed piece), or a tool called
    private bool _spokeThisTurn;
    private bool _toolCalledThisTurn;
    private bool _turnFromKeypad;

    // What the flow last said about keeping the agent's words uninterrupted, read on the turn
    // (AgentTurnCallbacks.KeepUninterrupted reads the conversation's states, which only the turn may touch). Kept across
    // turns: until a turn has read it, the last turn's answer is the best guess.
    private bool _keptUninterrupted;

    // What the flow said, as the last reply came in, about a caller going quiet (ConversationRelayStates.QuietWait and
    // GoodbyeOnSilence): kept until a later reply says otherwise, as a turn that says nothing still reads them
    private bool _quietWait;
    private string? _goodbyeOnSilence;

    // The caller went on talking before the current turn said anything: its reply is dropped, and their words so far
    // are answered together in the next turn
    private bool _superseded;

    // The last line said once its turn was over (text a tool gave), and whether the caller cut into it; the next turn is
    // told (AgentTurnCallbacks.LastReplyCutOff), as the history keeps that text whole. The line is kept past the next
    // turn's start: Twilio may still be playing it while that turn runs.
    private string? _afterTurnLine;
    private DateTime _afterTurnLineSentUtc;
    // _saidCount once the line went out: anything said since moves it on, and an interrupt is then not the line's
    private int _afterTurnLineMark;
    private bool _cutAfterTurn;
    // With _cutAfterTurn, what Twilio had played of the line when the caller cut in; null when it didn't say
    private string? _heardOfAfterTurnLine;

    // Bumped on everything sent for the caller to hear (a line, a streamed piece, a filler, a prompt)
    private int _saidCount;

    // The agent's last line (a reply or a line a tool gave, not a filler or a prompt) and when it went out: what an idle
    // prompt asks again, and what a caller's words that came too soon after it are checked against
    private string? _lastLine;
    private DateTime _lastLineSentUtc;
    // The last line plays to the end whoever speaks over it, so Twilio reports no cut-in into it
    private bool _lastLineKeptUninterrupted;
    // The caller heard a few words of the last line before cutting in: what they say next is meant for it
    private bool _lastLineHeardSome;

    // What was said, newest last, with _saidCount as it went out: tells which utterance Twilio says was playing at a
    // cut-in. Only the last few are kept.
    private readonly List<(string Text, int Mark)> _utterances = [];
    private const int UtterancesKept = 8;

    // Twilio's speaker events (the events attribute with speaker-events): whether it last said the agent is speaking
    private bool _agentSpeaking;
    // The lines said since Twilio last said the playback stopped (all of it played, or the caller cut in), with whether
    // each went out to play to the end and when: the ones to say again on a new session if the playback gets stuck
    // (ReconnectIfStuckAsync). Not fillers or idle prompts, which were about a moment that has passed. Kept only while
    // watching for a stuck playback, and all of them, unlike _utterances: a stuck playback plays none of what went out
    // after it, however much that is.
    private readonly List<(string Text, bool KeptUninterrupted, DateTime SentUtc)> _unheard = [];
    // A line that went out this soon before Twilio said the playback stopped may have crossed that message: Twilio
    // stopped before the line came, and plays it next. A line it played in that playback went out at least as long before
    // as it takes to play, which is longer for nearly any line; one that isn't is only said again needlessly.
    private static readonly TimeSpan CrossedPlaybackStop = TimeSpan.FromSeconds(1);
    // When everything said should have finished playing, by its length (EstimateSpeech)
    private DateTime _playedByUtc = DateTime.MinValue;
    // When the welcome greeting should have finished playing (by its length, or when Twilio said the playback stopped);
    // it can't be cut into, so what the caller says over it is answered after it. MinValue: no greeting, or it is over.
    private DateTime _welcomeEndsUtc = DateTime.MinValue;
    // How many times the call's relay has been connected again (ConversationRelayParameters.Reconnects)
    private int _reconnects;

    // The line the caller cut into, to say again once nothing follows the cut-in (ResumeAfterCutInSeconds)
    private string? _resumeLine;
    private DateTime _cutInUtc;
    private int _cutInActivity;
    // The last line said was a line said again: cut into again, it isn't said a third time
    private bool _lastWasResume;

    private string? _callSid;
    private string? _from;
    private string? _to;
    private string? _direction;

    /// <param name="sendCallerMessage">
    /// Sends the caller's words to the agent and returns its reply; the callbacks receive tool-call indications
    /// and the streamed reply
    /// </param>
    /// <param name="keepSaidLine">
    /// Keeps a line the session said on its own in the conversation history, with what it was
    /// (<see cref="ConversationRelayLabels"/>) and when it was said, so the history shows what the caller heard: the
    /// apology of a failed turn, an idle prompt and its goodbye, a line said again after a cut-in, and the fillers of a
    /// turn that kept none. A turn keeps its own reply, and a tool-call indication is the flow's to keep.
    /// </param>
    /// <param name="keepResume">
    /// Keeps what the session taking this one's place starts with (a <see cref="ConversationRelayResume"/> as JSON) when the
    /// playback is stuck, for Twilio's callback to hand on (<see cref="ConversationRelayResumes"/>)
    /// </param>
    public ConversationRelaySession(
        WebSocket socket,
        string agentId,
        string conversationId,
        TwilioSetting settings,
        Func<CallerMessage, AgentTurnCallbacks, Task<AssistantMessage?>> sendCallerMessage,
        ILogger logger,
        Func<string, string, DateTime, Task>? keepSaidLine = null,
        Func<string, Task>? keepResume = null,
        Func<string, string>? formatSpeech = null)
    {
        _socket = socket;
        _agentId = agentId;
        _conversationId = conversationId;
        _settings = settings;
        _relay = settings.ConversationRelay.For(agentId);
        _sendCallerMessage = sendCallerMessage;
        _logger = logger;
        _keepSaidLine = keepSaidLine;
        _keepResume = keepResume;
        _formatSpeech = formatSpeech;
    }

    private readonly Func<string, Task>? _keepResume;
    // Rewrites the agent's reply into how the voice should say it as it goes out; null: said as the model wrote it
    private readonly Func<string, string>? _formatSpeech;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var idleWatch = WatchIdleAsync(cts.Token);
        try
        {
            await ReceiveLoopAsync(cts.Token);
        }
        // ObjectDisposedException: the socket dropped on purpose under a pending receive (ReconnectIfStuckAsync)
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            _logger.LogInformation($"ConversationRelay socket closed for conversation {_conversationId}: {ex.Message}");
        }
        finally
        {
            // The call is gone: stop taking turns before waiting on anything, so a turn that finishes
            // meanwhile cannot start another one from the queue
            int dropped;
            lock (_sync)
            {
                _ended = true;
                dropped = _pendingInputs.Count;
                _pendingInputs.Clear();
            }

            cts.Cancel();
            await SwallowAsync(idleWatch);

            if (dropped > 0)
            {
                _logger.LogInformation($"Dropped {dropped} caller input(s) received after the agent's last turn started; " +
                    $"conversation {_conversationId} has ended.");
            }
        }

        // Let a turn already running finish so the conversation is saved, as well as one that ran past the timeout
        await SwallowAsync(_processing);
        await SwallowAsync(_timedOutTurn);
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
                var reconnects = 0;
                string? resume = null;
                if (root.TryGetProperty("customParameters", out var parameters))
                {
                    if (GetString(parameters, ConversationRelayParameters.WelcomeGreetingLength) is string length)
                    {
                        int.TryParse(length, out greetingLength);
                    }
                    if (GetString(parameters, ConversationRelayParameters.Reconnects) is string count)
                    {
                        int.TryParse(count, out reconnects);
                    }
                    resume = GetString(parameters, ConversationRelayParameters.Resume);
                }
                lock (_sync)
                {
                    _quietSinceUtc = DateTime.UtcNow + EstimateSpeech(greetingLength);
                    _playedByUtc = _quietSinceUtc;
                    _welcomeEndsUtc = greetingLength > 0 ? _quietSinceUtc : DateTime.MinValue;
                    _reconnects = reconnects;
                }
                _logger.LogInformation(
                    $"ConversationRelay session started for conversation {_conversationId} (call {_callSid}).");
                if (resume != null)
                {
                    await ResumeAsync(resume);
                }
                break;

            case "info":
                // Twilio's speaker events. One "on" and one "off" cover everything played back to back, however many lines
                // went out meanwhile: at "off" all of it has played.
                if (GetString(root, "name") == "agentSpeaking")
                {
                    lock (_sync)
                    {
                        _agentSpeaking = GetString(root, "value") == "on";
                        if (!_agentSpeaking)
                        {
                            var now = DateTime.UtcNow;
                            NotePlaybackStopped(now);
                            // Watching for a stuck playback, the caller's silence starts no sooner than the playback ended
                            // (the estimate may be short of it): no idle prompt was said while it played (WatchIdleAsync)
                            if (_relay.StuckPlaybackSeconds != 0 && _quietSinceUtc != DateTime.MaxValue && _quietSinceUtc < now)
                            {
                                _quietSinceUtc = now;
                            }
                        }
                    }
                }
                break;

            case "prompt":
                var text = GetString(root, "voicePrompt") ?? string.Empty;
                var last = !root.TryGetProperty("last", out var lastElement) || lastElement.ValueKind != JsonValueKind.False;
                if (!last)
                {
                    // An unfinalized transcript (partialPrompts, or an eager end of turn): the caller is mid-sentence, so
                    // not silent, not to be asked to repeat, and not to have a line said again over them. Its words are
                    // not kept: the final prompt carries the whole turn
                    if (text.Length > 0)
                    {
                        lock (_sync)
                        {
                            _quietSinceUtc = CallerActiveUtc();
                            _activity++;
                        }
                    }
                    break;
                }

                var prompt = text.Trim();
                // Only punctuation (".") is no words at all: answered, it took the place of the reply to the caller's real
                // words right after it (seen on a call)
                if (prompt.Any(char.IsLetterOrDigit))
                {
                    Enqueue(prompt, digits: null);
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
                bool sayAgain;
                lock (_sync)
                {
                    _quietSinceUtc = DateTime.UtcNow;
                    // The playback stopped where the caller cut in: what is said again after a cut-in is the cut-in's to
                    // decide (ResumeAfterCutInSeconds), not a stuck playback's
                    _agentSpeaking = false;
                    NotePlaybackStopped(DateTime.UtcNow);

                    // The caller talked over a reply still being streamed: don't send the rest of it. A stream already
                    // dropped (the turn timed out and the apology is playing, or the caller cut in before) or a
                    // session that ended is not cut in on again
                    if (_isProcessing && _streamedThisTurn && !_dropStream && !_ended)
                    {
                        _dropStream = true;
                        _cutInMidReply = true;
                        _heardBeforeCutIn = GetString(root, "utteranceUntilInterrupt");
                    }

                    var heard = GetString(root, "utteranceUntilInterrupt");
                    if (_afterTurnLine != null && !_ended && CutIntoAfterTurnLine(heard))
                    {
                        _cutAfterTurn = true;
                        _heardOfAfterTurnLine = HeardOfAfterTurnLine(heard);
                    }

                    // A few words of the line heard and what the caller says next answers it, however soon it comes.
                    // What was playing may be something else (a filler): words of that are no part of the line. A line
                    // still being streamed is the one cut into, as far as it went out
                    var heardOfLine = heard?.Trim();
                    var lineCutInto = _utteranceOpen ? _streamSegment.ToString().Trim() : _lastLine;
                    _lastLineHeardSome = !string.IsNullOrEmpty(heardOfLine) && lineCutInto != null
                        && lineCutInto.StartsWith(heardOfLine, StringComparison.Ordinal) && CountWords(heardOfLine) >= HeardSomeWords;
                    _utteranceCutIn |= _utteranceOpen;
                    NoteCutIn();
                    sayAgain = _resumeLine != null;
                }
                var interruptedAfter = GetString(root, "durationUntilInterruptMs");
                _logger.LogInformation(
                    $"Caller interrupted the agent in conversation {_conversationId} after {interruptedAfter} ms" +
                    (sayAgain ? $"; the line is said again if nothing follows for {_relay.ResumeAfterCutInSeconds} s." : "."));
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
        AgentTurnCallbacks? supersede = null;
        lock (_sync)
        {
            if (_ended)
            {
                return;
            }

            if (digits == null && IsBackchannelTail(content))
            {
                _logger.LogInformation($"Dropped \"{content}\" in conversation {_conversationId}: an acknowledgement that came in " +
                    "before the caller could hear the question just asked, the tail of what they said before or a word said " +
                    "to what came before the question.");
                // Its unfinalized words were no turn of the caller's either: a line it cut into is said again once they are
                // quiet (ResumeAfterCutInSeconds). Why: "That's right." after "Yes." cut a question off at its first word,
                // and the question was never said again (a dev call)
                if (_resumeLine != null)
                {
                    _cutInActivity = _activity;
                    _cutInUtc = DateTime.UtcNow;
                }
                return;
            }

            if (digits == null && IsGreetingDuringWelcome(content))
            {
                _logger.LogInformation($"Dropped \"{content}\" in conversation {_conversationId}: a greeting said over the " +
                    "welcome greeting, which already says what a reply would.");
                return;
            }

            _pendingInputs.Enqueue(new PendingInput(content, digits, DateTime.UtcNow));
            _quietSinceUtc = CallerActiveUtc();
            _idlePrompts = 0;
            _activity++;

            if (!_isProcessing)
            {
                _isProcessing = true;
                _processing = Task.Run(ProcessPendingAsync);
            }
            else if (digits == null && CanSupersede())
            {
                _superseded = true;
                _dropStream = true;
                supersede = _turnCallbacks;
                // The turn taking its place is told instead: the dropped turn said nothing that replaced the line
                if (supersede!.LastReplyCutOff)
                {
                    _cutAfterTurn = true;
                    _heardOfAfterTurnLine ??= supersede.LastReplyHeard;
                }
            }
        }

        if (supersede != null)
        {
            _logger.LogInformation($"Caller went on talking before the agent replied in conversation {_conversationId}; " +
                "dropping that reply and answering all they said.");
            // Outside the lock: cancelling runs the chat provider's cancellation inline
            supersede.Cancel();
        }
    }

    /// <summary>
    /// When the caller's silence is measured from after they speak: now, but no sooner than the end of the welcome greeting.
    /// Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// Why: the greeting can't be cut into, so a caller who speaks over it is answered after it. Measured from now, the
    /// silence began before the greeting ended and the reply that waits behind it was counted as already played: the idle
    /// prompt ("I didn't get anything") came right behind the reply.
    /// </remarks>
    private DateTime CallerActiveUtc()
    {
        var now = DateTime.UtcNow;
        return _welcomeEndsUtc > now ? _welcomeEndsUtc : now;
    }

    // Words that only greet, and the ones a greeting needs at least one of
    private static readonly HashSet<string> GreetingWords = new(StringComparer.Ordinal)
    {
        "hello", "hi", "hey", "hiya", "good", "morning", "afternoon", "evening", "there", "uh", "um", "oh"
    };

    private static readonly HashSet<string> GreetingOpeners = new(StringComparer.Ordinal)
    {
        "hello", "hi", "hey", "hiya", "morning", "afternoon", "evening"
    };

    private const int MaxGreetingWords = 4;

    /// <summary>
    /// Whether these words only greet, came in while the welcome greeting was playing (or within TooSoonToAnswer after it),
    /// and the agent has said nothing of its own yet (DropGreetingsDuringWelcome). Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// Why: the greeting can't be cut into, and the caller's "Hello?" over it was taken as a turn and answered with the
    /// greeting's own words again, right after it. Only greetings are dropped: anything more, such as "Hello, this is ...",
    /// may be a voicemail greeting or an answer.
    /// </remarks>
    private bool IsGreetingDuringWelcome(string content)
    {
        if (!_relay.DropGreetingsDuringWelcome || _welcomeEndsUtc == DateTime.MinValue || _saidCount > 0
            || DateTime.UtcNow >= _welcomeEndsUtc + TooSoonToAnswer)
        {
            return false;
        }

        var words = Words(content);
        return words.Count is > 0 and <= MaxGreetingWords
            && words.All(GreetingWords.Contains) && words.Any(GreetingOpeners.Contains);
    }

    // Words that acknowledge
    private static readonly HashSet<string> Acknowledgements = new(StringComparer.Ordinal)
    {
        "yes", "yeah", "yep", "okay", "ok", "sure", "agree", "fine", "good", "right", "correct", "alright", "great", "thanks",
        "thank", "perfect", "sounds", "mm-hmm", "uh-huh"
    };

    // Words that come with one and say nothing alone ("I agree.", "That's fine.", "Thank you."): "Is that it?" is a question
    private static readonly HashSet<string> AcknowledgementFillers = new(StringComparer.Ordinal)
    {
        "i", "that's", "that", "is", "it", "you", "uh", "um", "so"
    };

    // A caller who heard this many words of a line before cutting in was answering it
    private const int HeardSomeWords = 3;

    /// <summary>
    /// Whether these words are the tail of what the caller said before the last line went out: they only acknowledge,
    /// came in too soon after that line to be about it (for a line played to the end, before its question could have
    /// been heard), and the line asks something (DropBackchannelTails). Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// Why: the transcription splits an utterance in two ("Yes." then "I agree."); the second part came in after the next
    /// question went out and was taken as its answer (seen on calls).
    /// </remarks>
    private bool IsBackchannelTail(string content)
    {
        if (!_relay.DropBackchannelTails || _lastLineHeardSome || _lastLine == null || !_lastLine.Contains('?')
            || DateTime.UtcNow >= _lastLineSentUtc + TooSoonToAnswer + UntilQuestionPlays())
        {
            return false;
        }

        var words = Words(content);
        // "Is that right" asks, whether or not the transcript gave it its question mark
        var asks = content.Contains('?') || words.SkipWhile(w => w is "uh" or "um" or "so").FirstOrDefault() == "is";
        return !asks && words.Any(Acknowledgements.Contains)
            && words.All(w => Acknowledgements.Contains(w) || AcknowledgementFillers.Contains(w));
    }

    /// <summary>
    /// How long after the last line went out its question starts playing, when the line plays to the end; zero for a line
    /// the caller may cut into. Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// Why: "Perfect.", said to "Someone will be out within the next three days." while the rest of the line ("... I just
    /// texted it to you. Can you check if you got it?") still played, came in 2.8 s after the line went out and was taken
    /// as the link checked; the request number went out next, before the caller heard the question (dev call 4bcd88e1).
    /// A line the caller may cut into is left as it was: Twilio stops it where they speak and says what they heard.
    /// </remarks>
    private TimeSpan UntilQuestionPlays()
    {
        return _lastLineKeptUninterrupted
            ? TimeSpan.FromSeconds(ConversationRelayLines.BeforeFirstQuestion(_lastLine).Length / FastestSpokenCharsPerSecond)
            : TimeSpan.Zero;
    }

    private static List<string> Words(string? text)
    {
        return Regex.Replace((text ?? string.Empty).ToLowerInvariant().Replace('’', '\''), @"[^a-z0-9'\- ]", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static int CountWords(string? text) => Words(text).Count;

    /// <summary>
    /// The current turn can still be taken back for what the caller says next: it streams (so it can be cancelled),
    /// was spoken, not keyed in, has neither said anything nor called a tool, and the flow isn't keeping what the agent
    /// says uninterrupted. Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// The caller paused mid-thought and Twilio took that for the end of what they said: answering the first half
    /// would talk over the rest, which then comes in as a cut-in. A flow that keeps the agent's words uninterrupted
    /// has a line the caller must hear coming (a confirmation number), and may fetch it in code, where no tool call
    /// shows.
    /// </remarks>
    private bool CanSupersede()
    {
        return _relay.StreamReplies && _turnCallbacks != null && !_turnFromKeypad
            && !_turnReplied && !_superseded && !_streamedThisTurn && !_spokeThisTurn && !_toolCalledThisTurn
            && !_keptUninterrupted;
    }

    /// <summary>
    /// Whether the flow keeps what the agent says now uninterrupted; call on the turn only
    /// </summary>
    private bool ReadKeepUninterrupted()
    {
        var keep = _turnCallbacks?.KeepUninterrupted?.Invoke() == true;
        lock (_sync)
        {
            _keptUninterrupted = keep;
        }
        return keep;
    }

    private Task ReadFlowAtTurnStartAsync()
    {
        ReadKeepUninterrupted();
        return Task.CompletedTask;
    }

    private async Task StartStreamAsync()
    {
        ReadKeepUninterrupted();
        await CloseUtteranceAsync();
    }

    /// <summary>
    /// Runs one agent turn at a time; what the caller says meanwhile is merged into the next turn.
    /// A keypad entry ends the merge, so each entry keeps its own Digits. A turn that has done nothing the caller
    /// would notice yet is cancelled when they go on talking (<see cref="CanSupersede"/>), and the next turn answers
    /// what they said with the earlier words in its history.
    /// </summary>
    private async Task ProcessPendingAsync()
    {
        while (true)
        {
            var stream = _relay.StreamReplies;
            var callbacks = new AgentTurnCallbacks
            {
                OnIndication = SpeakToolIndicationAsync,
                OnTurnStarted = ReadFlowAtTurnStartAsync,
                OnStreamText = stream ? StreamTextAsync : null,
                OnStreamStarted = stream ? StartStreamAsync : null,
                // A line said before a slow tool ("Okay, that's everything I need") is spoken now, and the watchdog
                // counts the silence after it instead of waiting for the next response to close it
                OnStreamEndedWithToolCall = stream ? CloseUtteranceBeforeToolAsync : null,
                // Streamed or not: a reply the call ended before is marked as never said
                RewriteReply = MarkWhatTheCallerCutOff
            };

            CallerMessage message;
            lock (_sync)
            {
                if (_ended || _pendingInputs.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }

                var batch = new List<PendingInput>();
                while (_pendingInputs.TryDequeue(out var input))
                {
                    batch.Add(input);
                    if (input.Digits != null)
                    {
                        break;
                    }
                }

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
                _speechHeld.Clear();
                _streamSpokenChars = 0;
                _lastStreamSegment = string.Empty;
                _utteranceOpen = false;
                _streamedThisTurn = false;
                _dropStream = false;
                _cutInMidReply = false;
                _heardBeforeCutIn = null;
                _turnReplied = false;
                _turnStartedUtc = DateTime.UtcNow;
                _spokeThisTurn = false;
                _toolCalledThisTurn = false;
                _turnFromKeypad = message.Digits != null;
                _superseded = false;
                // Words that came in before the line went out (said while the last turn worked) can't answer it either,
                // nor words that came in too soon after it to be about it (TooSoonToAnswer)
                callbacks.LastReplyCutOff = _cutAfterTurn
                    || _afterTurnLine != null && batch.Any(input => input.ReceivedUtc < _afterTurnLineSentUtc + TooSoonToAnswer);
                if (_cutAfterTurn && _heardOfAfterTurnLine != null)
                {
                    callbacks.LastReplyLine = _afterTurnLine;
                    callbacks.LastReplyHeard = _heardOfAfterTurnLine;
                }
                _cutAfterTurn = false;
                _heardOfAfterTurnLine = null;
                // Set with the flags above, so caller input arriving now never sees the last turn's callbacks
                _turnCallbacks = callbacks;
            }

            using var watchdogCts = new CancellationTokenSource();
            var watchdog = _relay.IsResponseWatchdogEnabledFor(_agentId)
                ? WatchResponseAsync(message.SeqNumber, callbacks, watchdogCts.Token)
                : Task.CompletedTask;

            AssistantMessage? reply;
            try
            {
                // Run the turn outside the WebSocket request's execution context; it sets up its own HttpContext
                Task<AssistantMessage?> turn;
                using (ExecutionContext.SuppressFlow())
                {
                    turn = Task.Run(() => _sendCallerMessage(message, callbacks));
                }

                var timeout = Task.Delay(TimeSpan.FromSeconds(_relay.AiResponseTimeoutSeconds));
                if (await Task.WhenAny(turn, timeout) != turn)
                {
                    var timeoutSeconds = _relay.AiResponseTimeoutSeconds;
                    _logger.LogWarning($"Agent did not reply within {timeoutSeconds}s in conversation {_conversationId}.");
                    _timedOutTurn = turn;
                    await FailAsync(callbacks);
                    return;
                }
                reply = await turn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Agent turn failed in conversation {_conversationId}.");
                await FailAsync(callbacks);
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
                await KeepFillersLeftAsync(callbacks);
            }

            await CloseUtteranceAsync();
            if (reply != null)
            {
                // Before a reply with nothing to say is passed over: a flow waiting quietly answers the caller's asides
                // with nothing
                lock (_sync)
                {
                    _quietWait = reply.QuietWait;
                    _goodbyeOnSilence = reply.GoodbyeOnSilence;
                }
            }
            if (reply == null
                || string.IsNullOrWhiteSpace(reply.Content) && !reply.ConversationEnd && !reply.HumanIntervationNeeded)
            {
                continue;
            }

            bool superseded;
            lock (_sync)
            {
                superseded = _superseded;
                _keptUninterrupted = reply.KeepUninterrupted;
            }
            // A transfer or hang-up still goes ahead: the caller's next words would not change it
            if (superseded && !reply.ConversationEnd && !reply.HumanIntervationNeeded)
            {
                continue;
            }

            // Noted once the send lock is held, as it goes out: an interrupt of something said before it isn't its
            // Kept as it is said: Twilio's report of what played is in the voice's words
            var unstreamed = Speech(GetUnstreamedText(reply.Content));
            await SpeakAsync(unstreamed, canSpeak: () =>
            {
                _afterTurnLine = unstreamed;
                _afterTurnLineSentUtc = DateTime.UtcNow;
                // MarkSpeaking, right after this, counts the line itself
                _afterTurnLineMark = _saidCount + 1;
                NoteLine(unstreamed, keptUninterrupted: reply.KeepUninterrupted);
                return true;
            }, keepUninterrupted: reply.KeepUninterrupted);
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
    /// said twice in a turn while unused ones remain. Each one said is noted on the turn's callbacks (NoteFillerSaid), to be
    /// kept in the history.
    /// </summary>
    private async Task WatchResponseAsync(int seqNumber, AgentTurnCallbacks callbacks, CancellationToken cancellationToken)
    {
        var relay = _relay;
        var said = new HashSet<string>();
        var step = 0;
        while (true)
        {
            var interval = TimeSpan.FromSeconds(relay.ResponseWatchdogIntervals[Math.Min(step, relay.ResponseWatchdogIntervals.Count - 1)]);
            await Task.Delay(WatchdogPollInterval, cancellationToken);
            lock (_sync)
            {
                if (IsTurnOver(seqNumber))
                {
                    return;
                }
                if (!IsSilentFor(interval))
                {
                    continue;
                }
            }

            var text = PickFillerText(step == 0 ? relay.ResponseWatchdogPrompts : relay.ResponseWatchdogLongPrompts,
                step == 0 ? DefaultWatchdogPrompts : DefaultWatchdogLongPrompts, said);
            if (await SpeakFillerAsync(seqNumber, text, interval, callbacks))
            {
                said.Add(text);
                step++;
            }
        }
    }

    private async Task<bool> SpeakFillerAsync(int seqNumber, string text, TimeSpan interval, AgentTurnCallbacks callbacks)
    {
        // Decided again once the send lock is held: while waiting for it the reply may have come in, or a streamed
        // line may have started or been closed, and then the silence this filler was meant for is over
        return await SpeakIfAsync(() =>
            {
                if (IsTurnOver(seqNumber) || !IsSilentFor(interval))
                {
                    return false;
                }
                RememberUtterance(text, _saidCount + 1, again: false);
                // Noted as it goes out, under the send lock: a turn that fails meanwhile finds every filler said before
                // its apology
                callbacks.NoteFillerSaid(text);
                return true;
            },
            new { type = "text", token = text, last = true }, EstimateSpeech(text.Length));
    }

    /// <summary>
    /// Notes the agent's line that just went out (<see cref="_lastLine"/>). Call under <see cref="_sync"/>.
    /// </summary>
    /// <param name="keptUninterrupted">The line went out to play to the end whoever speaks over it</param>
    private void NoteLine(string? line, DateTime? sentUtc = null, bool keepHeard = false, bool keptUninterrupted = false)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }
        _lastLine = line.Trim();
        _lastLineSentUtc = sentUtc ?? DateTime.UtcNow;
        _lastLineKeptUninterrupted = keptUninterrupted;
        if (!keepHeard)
        {
            _lastLineHeardSome = false;
        }
        _lastWasResume = false;
        // A line cut into before this one went out is not said again after it
        _resumeLine = null;
    }

    /// <summary>
    /// Notes something said, with the _saidCount it went out at. Call under <see cref="_sync"/>.
    /// </summary>
    /// <param name="keptUninterrupted">It went out to play to the end whoever speaks over it</param>
    /// <param name="again">Said again on a new session if it never finished playing (ReconnectIfStuckAsync): not for a
    /// filler or an idle prompt, which were about a moment that has passed</param>
    private void RememberUtterance(string text, int mark, bool keptUninterrupted = false, bool again = true)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        _utterances.Add((text.Trim(), mark));
        if (_utterances.Count > UtterancesKept)
        {
            _utterances.RemoveAt(0);
        }
        if (again && _relay.StuckPlaybackSeconds != 0)
        {
            _unheard.Add((text.Trim(), keptUninterrupted, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Twilio said the playback stopped: all of it played, or the caller cut in. Call under <see cref="_sync"/>.
    /// </summary>
    private void NotePlaybackStopped(DateTime now)
    {
        // The welcome greeting, if it was still playing by the estimate, is over
        if (_welcomeEndsUtc > now)
        {
            _welcomeEndsUtc = now;
        }
        // What went out before it is over, but for a line that may have crossed Twilio's message (CrossedPlaybackStop)
        _unheard.RemoveAll(line => line.SentUtc < now - CrossedPlaybackStop);
        // Nothing is left to play but such a line: what goes out next plays after it, not after what the estimate still
        // had to come. Without it, a long line that crossed the message would be taken as stuck while it still plays.
        _playedByUtc = now + _unheard.Aggregate(TimeSpan.Zero, (sum, line) => sum + EstimateSpeech(line.Text.Length));
    }

    /// <summary>
    /// The turn the response watchdog was started for has its reply, or the call moved on. Call under <see cref="_sync"/>.
    /// </summary>
    private bool IsTurnOver(int seqNumber)
    {
        return _ended || _seqNum != seqNumber || _turnReplied || _superseded;
    }

    /// <summary>
    /// Nothing has been said, by either side, for <paramref name="interval"/>. Call under <see cref="_sync"/>.
    /// </summary>
    private bool IsSilentFor(TimeSpan interval)
    {
        // _quietSinceUtc is the estimated end of what was last said, or the caller's last input
        var quietSince = _quietSinceUtc == DateTime.MaxValue || _quietSinceUtc < _turnStartedUtc ? _turnStartedUtc : _quietSinceUtc;
        return !_utteranceOpen && DateTime.UtcNow - quietSince >= interval;
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
        var relay = _relay;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            if (await ReconnectIfStuckAsync())
            {
                return;
            }
            await ResumeIfSilentAsync();

            var askAgain = false;
            var hangUp = false;
            var activity = 0;
            var stage = 0;
            string? question = null;
            var quietWait = false;
            string? goodbyeOnSilence = null;
            lock (_sync)
            {
                var quietFor = DateTime.UtcNow - _quietSinceUtc;
                // Watching for a stuck playback, a caller Twilio is still playing to isn't silent: an idle prompt then would
                // only queue behind the playback, and each one would put off finding it stuck until the idle prompts ran out
                // and hung up (ReconnectIfStuckAsync)
                var stillPlaying = _relay.StuckPlaybackSeconds != 0 && _agentSpeaking;
                if (_ended || _isProcessing || _quietSinceUtc == DateTime.MaxValue || _resumeLine != null || stillPlaying
                    || quietFor < IdleTimeout(_idlePrompts, _quietWait))
                {
                    continue;
                }

                stage = _idlePrompts;
                quietWait = _quietWait;
                goodbyeOnSilence = _goodbyeOnSilence;
                if (_idlePrompts < (quietWait ? relay.MaxQuietWaitPrompts : relay.MaxIdlePrompts))
                {
                    _idlePrompts++;
                    askAgain = true;
                    activity = _activity;
                    question = !quietWait && relay.IdleAsksAgain && stage == 0 ? ConversationRelayLines.LastQuestion(_lastLine) : null;
                }
                else
                {
                    // Decide and end in one step, so caller input arriving now cannot start a turn on a call being hung up
                    _ended = true;
                    hangUp = true;
                }
                _quietSinceUtc = DateTime.UtcNow;
            }

            // Rechecked once the send lock is held: caller input arriving meanwhile starts a turn, and the prompt must not
            // then be said over or after that turn's reply
            bool StillSilent() => !_isProcessing && _activity == activity;
            // The caller is busy on their phone: nothing to say again and no "Are you still there?", only that the agent
            // is still on the line. Why: the previous build read that silence as a dead call and hung up on callers
            // filling in the form it had texted them
            if (askAgain && quietWait)
            {
                await SayOwnLineAsync(PromptText("quiet-wait-check-in", DefaultQuietWaitCheckInText), ConversationRelayLabels.IdlePrompt,
                    StillSilent);
            }
            else if (askAgain && question != null)
            {
                await SayOwnLineAsync(PromptText("say-it-again-question", DefaultAskAgainText).Replace("{0}", question),
                    ConversationRelayLabels.IdlePrompt, StillSilent);
            }
            // Nothing heard from the caller yet (no words, no keys): there is nothing for them to say again, only whether
            // they are there. Why: silence after the greeting got "Sorry, can you say it again?" (local call simulation)
            else if (askAgain && (relay.IdleAsksAgain && stage > 0 || activity == 0))
            {
                await SayOwnLineAsync(PromptText("still-there", DefaultStillThereText), ConversationRelayLabels.IdlePrompt, StillSilent);
            }
            else if (askAgain)
            {
                await SayOwnLineAsync(PromptText($"say-it-again-{Random.Shared.Next(1, 5)}", DefaultSayItAgainText),
                    ConversationRelayLabels.IdlePrompt, StillSilent);
            }
            else if (hangUp)
            {
                _logger.LogInformation($"Caller stayed silent, hanging up conversation {_conversationId}.");
                string? goodbye = null;
                var goodbyeUtc = DateTime.MinValue;
                // The flow's own goodbye is said whether or not the idle prompts say one: it tells what the caller still has
                // to hear, such as a confirmation number
                if (goodbyeOnSilence != null || relay.IdleAsksAgain)
                {
                    // The session is marked ended already, so nothing else can be said after it, and nothing the caller
                    // says over it is answered: it plays to the end
                    goodbye = goodbyeOnSilence ?? PromptText("idle-goodbye", DefaultIdleGoodbyeText);
                    await SendAsync(TextFrame(goodbye, last: true, keepUninterrupted: true));
                    goodbyeUtc = DateTime.UtcNow;
                }
                await SendEndAsync(ConversationRelayHandoff.HangUp);
                // Kept once the call is ended, so the storage doesn't hold up the hang-up
                if (goodbye != null)
                {
                    await KeepSaidAsync(goodbye, ConversationRelayLabels.IdleGoodbye, goodbyeUtc);
                }
            }
        }
    }

    /// <summary>
    /// Ends a session whose playback is stuck (ConversationRelaySetting.StuckPlaybackSeconds) and has Twilio connect the call
    /// again: the new session says the lines that never finished playing (<see cref="ResumeAsync"/>). Whether it did.
    /// </summary>
    /// <remarks>
    /// Stuck: Twilio still says the agent is speaking well after everything said should have finished playing, and no turn
    /// is running (one still running may say more, which then goes with the rest). Only while the call may still be
    /// connected again (MaxReconnects): past that the action callback would hang it up.
    /// Why say the lines again rather than carry on: a stuck playback plays nothing queued behind it and lets none of the
    /// caller's words through, so the caller heard neither the lines nor anything after them (dev calls 1c28cefa, 46bc439c).
    /// Why the socket is dropped rather than the session ended: Twilio acts on an "end" message only once what is queued has
    /// played (dev: the session ended 5.4-6.0 s after a goodbye and its "end" went out together, the goodbye's length), and a
    /// stuck playback never has. A dropped socket ends it at once: Twilio's callback then finds the session failed, as when
    /// an instance holding a socket goes away, and connects the call again with what was kept (<see cref="ConversationRelayResumes"/>).
    /// </remarks>
    private async Task<bool> ReconnectIfStuckAsync()
    {
        ConversationRelayResume resume;
        double overdueSeconds;
        lock (_sync)
        {
            var stuckAfter = _relay.StuckPlaybackSeconds;
            if (stuckAfter == 0 || _ended || _isProcessing || !_agentSpeaking || _reconnects >= _relay.MaxReconnects)
            {
                return false;
            }
            var overdue = DateTime.UtcNow - _playedByUtc;
            if (overdue < TimeSpan.FromSeconds(stuckAfter))
            {
                return false;
            }

            // None when only a filler or an idle prompt went out since the playback last stopped: the new session then says
            // nothing until the caller does, or its own idle prompt
            var lines = _unheard
                .Select(line => new ConversationRelayResumeLine { Text = line.Text, KeepUninterrupted = line.KeptUninterrupted })
                .ToList();
            resume = new ConversationRelayResume { Lines = lines, QuietWait = _quietWait, GoodbyeOnSilence = _goodbyeOnSilence };
            overdueSeconds = overdue.TotalSeconds;
            // Ended in the same step, so caller input arriving now can't start a turn on a session being replaced
            _ended = true;
        }

        _logger.LogWarning($"ConversationRelay playback is stuck in conversation {_conversationId}: Twilio still says the " +
            $"agent is speaking {overdueSeconds:0.0} s after all of it should have played. Dropping the socket so the call is " +
            $"connected again, to say {resume.Lines.Count} line(s) again.");
        if (_keepResume != null)
        {
            try
            {
                await _keepResume(JsonSerializer.Serialize(resume));
            }
            catch (Exception ex)
            {
                // Connected again all the same: a new session that says nothing beats a stuck one, and its idle prompts ask
                _logger.LogWarning(ex, $"Could not keep what to say again in conversation {_conversationId}.");
            }
        }
        // Under the send lock, so no frame is going out as the socket drops
        await _sendLock.WaitAsync();
        try
        {
            _socket.Abort();
        }
        finally
        {
            _sendLock.Release();
        }
        return true;
    }

    /// <summary>
    /// Starts a session that takes the place of one whose playback was stuck (<see cref="ReconnectIfStuckAsync"/>): the
    /// lines the caller never heard all of are said again, each as it went out, and a silent caller is treated as the flow
    /// last said. They are in the history already, so they aren't kept again.
    /// </summary>
    private async Task ResumeAsync(string json)
    {
        ConversationRelayResume? resume;
        try
        {
            resume = JsonSerializer.Deserialize<ConversationRelayResume>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning($"Unreadable resume for conversation {_conversationId}; nothing is said again: {ex.Message}");
            return;
        }
        if (resume == null)
        {
            return;
        }

        lock (_sync)
        {
            _quietWait = resume.QuietWait;
            _goodbyeOnSilence = resume.GoodbyeOnSilence;
        }
        var said = 0;
        foreach (var line in resume.Lines)
        {
            if (await SpeakAsync(line.Text, canSpeak: () =>
                {
                    NoteLine(line.Text, keptUninterrupted: line.KeepUninterrupted);
                    return true;
                }, keepUninterrupted: line.KeepUninterrupted))
            {
                said++;
            }
        }
        _logger.LogInformation($"ConversationRelay session for conversation {_conversationId} took the place of one whose " +
            $"playback was stuck; said {said} line(s) again.");
    }

    // How long the caller may stay silent before the idle prompt after this many (the last is before the hang-up)
    private TimeSpan IdleTimeout(int prompts, bool quietWait)
    {
        if (quietWait)
        {
            return TimeSpan.FromSeconds(_relay.QuietWaitSeconds);
        }
        var timeouts = _relay.IdleTimeouts;
        return TimeSpan.FromSeconds(timeouts.Count > 0 ? timeouts[Math.Min(prompts, timeouts.Count - 1)] : _relay.IdleTimeoutSeconds);
    }

    private string PromptText(string name, string fallback)
    {
        return _settings.PromptTexts.TryGetValue(name, out var text) && !string.IsNullOrWhiteSpace(text) ? text : fallback;
    }

    /// <summary>
    /// The caller cut in and then said nothing: a noise, or the line's own voice coming back. Notes the line they cut into
    /// to say again (ResumeAfterCutInSeconds). Call under <see cref="_sync"/>, on the interrupt.
    /// </summary>
    /// <remarks>
    /// Only a line a tool gave, said after its turn (a question): the caller otherwise waits in silence for an answer to a
    /// question they never heard (seen on calls). A line said again and cut into again is not said a third time: the
    /// caller is talking.
    /// </remarks>
    private void NoteCutIn()
    {
        if (_relay.ResumeAfterCutInSeconds <= 0 || _ended || _lastWasResume || !_cutAfterTurn)
        {
            _resumeLine = null;
            return;
        }

        _resumeLine = _afterTurnLine;
        _cutInUtc = DateTime.UtcNow;
        _cutInActivity = _activity;
    }

    private async Task ResumeIfSilentAsync()
    {
        string? line;
        int activity;
        lock (_sync)
        {
            if (_resumeLine == null)
            {
                return;
            }
            // Anything from the caller since the cut-in (words, keys) is their turn: nothing is said again
            if (_ended || _activity != _cutInActivity)
            {
                // Why logged: after a cut-in the line never came back and the call sat silent, and nothing showed whether
                // the caller had started talking or the line was never set to be said again (a staging call, 2026-11-13)
                if (!_ended)
                {
                    _logger.LogInformation($"The caller spoke within {(DateTime.UtcNow - _cutInUtc).TotalSeconds:0.0} s of cutting in, " +
                        $"in conversation {_conversationId}; the line they cut into is not said again.");
                }
                _resumeLine = null;
                return;
            }
            if (_isProcessing || DateTime.UtcNow - _cutInUtc < TimeSpan.FromSeconds(_relay.ResumeAfterCutInSeconds))
            {
                return;
            }
            line = _resumeLine;
            activity = _activity;
            _resumeLine = null;
        }

        var said = await SpeakAsync(line, canSpeak: () =>
        {
            if (_isProcessing || _activity != activity)
            {
                return false;
            }
            // Said again in full: the caller has now heard it, so the next turn isn't told it was cut off
            _afterTurnLine = line;
            _afterTurnLineSentUtc = DateTime.UtcNow;
            _afterTurnLineMark = _saidCount + 1;
            _cutAfterTurn = false;
            _heardOfAfterTurnLine = null;
            NoteLine(line);
            // Set as it goes out: a cut-in into it, however soon, doesn't get it said a third time
            _lastWasResume = true;
            return true;
        });
        if (said)
        {
            _logger.LogInformation($"Nothing followed the caller's cut-in in conversation {_conversationId}; said the line again.");
            await KeepSaidAsync(line, ConversationRelayLabels.SaidAgain, DateTime.UtcNow);
        }
    }

    private async Task FailAsync(AgentTurnCallbacks callbacks)
    {
        // A turn that timed out may still be streaming; keep its words out of the apology
        lock (_sync)
        {
            _dropStream = true;
        }
        await CloseUtteranceAsync();
        var apology = PromptText("error", DefaultErrorText);
        // The call ends after it, so nothing the caller says over it is answered: it plays to the end
        var apologized = await SpeakAsync(apology, keepUninterrupted: true);
        var apologyUtc = DateTime.UtcNow;
        await EndAsync(ConversationRelayHandoff.HangUp);

        // Kept once the call is ended, so the storage doesn't hold up the hang-up and no filler can come after the
        // apology. The turn kept none of the fillers it took to fail: they go first, as they were said first.
        await KeepFillersLeftAsync(callbacks);
        if (apologized)
        {
            await KeepSaidAsync(apology, ConversationRelayLabels.Apology, apologyUtc);
        }
    }

    /// <summary>
    /// Keeps the fillers of a turn that it didn't keep itself: it failed, ran past its time, or stored no reply. Call once
    /// the turn can say nothing more.
    /// </summary>
    private async Task KeepFillersLeftAsync(AgentTurnCallbacks callbacks)
    {
        foreach (var (text, saidUtc) in callbacks.TakeFillers())
        {
            await KeepSaidAsync(text, ConversationRelayLabels.Filler, saidUtc);
        }
    }

    /// <summary>
    /// Says a line of the session's own, not the agent's, and keeps it in the history once it went out
    /// </summary>
    /// <param name="label">What the line is (<see cref="ConversationRelayLabels"/>)</param>
    /// <param name="canSpeak">Extra condition, checked under <see cref="_sync"/> once the send lock is held</param>
    private async Task SayOwnLineAsync(string text, string label, Func<bool>? canSpeak = null)
    {
        if (await SpeakAsync(text, canSpeak, sayAgainIfStuck: label != ConversationRelayLabels.IdlePrompt))
        {
            await KeepSaidAsync(text, label, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Keeps a line the session said on its own in the conversation history, so the history shows what the caller heard
    /// </summary>
    /// <param name="label">What the line is (<see cref="ConversationRelayLabels"/>)</param>
    private async Task KeepSaidAsync(string text, string label, DateTime saidUtc)
    {
        if (_keepSaidLine == null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            await _keepSaidLine(text.Trim(), label, saidUtc);
        }
        catch (Exception ex)
        {
            // Only the record of the line is lost: the call goes on
            _logger.LogWarning(ex, $"Could not keep a line said in conversation {_conversationId}.");
        }
    }

    /// <summary>
    /// Sends a piece of the reply as the model writes it; Twilio speaks it as it arrives.
    /// </summary>
    private async Task StreamTextAsync(string text)
    {
        if (_formatSpeech == null)
        {
            await SendStreamAsync(text, text);
            return;
        }

        // Held until the end of its clause, so the voice's rewrite sees whole words (ConversationRelaySpeech)
        string ready;
        lock (_sync)
        {
            _speechHeld.Append(text);
            string rest;
            (ready, rest) = ConversationRelaySpeech.SplitAtClauseEnd(_speechHeld.ToString());
            _speechHeld.Clear().Append(rest);
        }
        if (ready.Length > 0)
        {
            await SendStreamAsync(ready, Speech(ready));
        }
    }

    /// <summary>
    /// Sends what is still held of the reply, formatted, before the line is closed
    /// </summary>
    private async Task FlushSpeechAsync()
    {
        string rest;
        lock (_sync)
        {
            rest = _speechHeld.ToString();
            _speechHeld.Clear();
        }
        if (rest.Length > 0)
        {
            await SendStreamAsync(rest, Speech(rest));
        }
    }

    /// <summary>
    /// The text as the voice should say it. A hook that fails changes nothing: the caller hears the model's words.
    /// </summary>
    private string Speech(string text)
    {
        if (_formatSpeech == null || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        try
        {
            return _formatSpeech(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not format what the agent says in conversation {_conversationId}; it is said as written.");
            return text;
        }
    }

    /// <param name="text">What the model wrote, which the session keeps (the history, what was cut into)</param>
    /// <param name="spoken">What goes out to the voice for it</param>
    private async Task SendStreamAsync(string text, string spoken)
    {
        var keep = ReadKeepUninterrupted();

        // Decided once the send lock is held: a piece queued behind another send must not go out after the
        // caller cut in (_dropStream) or the session ended meanwhile
        await SendIfAsync(() =>
        {
            if (_ended || _dropStream)
            {
                return false;
            }
            if (!_utteranceOpen)
            {
                _utteranceStartMark = _saidCount + 1;
                _utteranceStartUtc = DateTime.UtcNow;
                _utteranceCutIn = false;
            }
            _streamSegment.Append(text);
            _streamSpokenChars += spoken.Length;
            _utteranceOpen = true;
            _streamedThisTurn = true;
            _saidCount++;
            _utteranceKeptUninterrupted = keep;
            return true;
        }, TextFrame(spoken, last: false, keep));
    }

    // A line the caller must hear goes out marked so: Twilio then plays it to the end, whoever speaks over it, which
    // overrides the interruptible attribute set for the call
    private static object TextFrame(string token, bool last, bool keepUninterrupted) => keepUninterrupted
        ? new { type = "text", token, last, interruptible = false }
        : new { type = "text", token, last };

    /// <summary>
    /// Ends the utterance being streamed, so what follows (an indication, the next response) is spoken on its own.
    /// </summary>
    private async Task CloseUtteranceAsync()
    {
        await FlushSpeechAsync();
        bool keep;
        lock (_sync)
        {
            if (!_utteranceOpen)
            {
                return;
            }
            keep = _utteranceKeptUninterrupted;
        }

        // Closed only once the send lock is held, so nothing else (a filler) can go out between the utterance being
        // marked closed and its closing frame, and the silence clock already counts the closed line when a filler
        // next looks at it
        await SendIfAsync(() =>
        {
            if (!_utteranceOpen)
            {
                return false;
            }
            _utteranceOpen = false;
            _lastStreamSegment = _streamSegment.ToString();
            _streamSegment.Clear();
            // By the length of what the voice says: "B16649128" spelled out takes longer to say than it is to write
            MarkSpeaking(EstimateSpeech(_streamSpokenChars));
            _streamSpokenChars = 0;
            RememberUtterance(_lastStreamSegment, _utteranceStartMark, keep);
            // Cut into while it streamed: the caller's words are about it, so it dates from when it started and keeps
            // what they heard of it, not from now (a "Yes." said over it was dropped as a tail, review)
            if (_utteranceCutIn)
            {
                NoteLine(_lastStreamSegment, _utteranceStartUtc, keepHeard: true);
            }
            else
            {
                NoteLine(_lastStreamSegment);
            }
            return true;
        }, TextFrame(string.Empty, last: true, keep));
    }

    /// <summary>
    /// What the conversation history keeps of a reply the caller cut in on: the part that went out, and after it a
    /// mark naming the rest, so the next turn knows the caller never heard it. Every other reply is kept as it is.
    /// </summary>
    /// <remarks>
    /// Only a reply cut off before the turn is over can be marked: once it is over, the history has taken the reply.
    /// Text goes out faster than it is spoken, so the cut is placed where Twilio says playback stopped; when Twilio
    /// doesn't say, or what it says is not the start of what went out, the cut falls where the sending stopped. A reply
    /// dropped because the caller went on talking first is marked as never said, so the next turn doesn't take it as
    /// heard, and so is one the call ended before: the caller hung up while the agent worked, or the turn ran past
    /// AiResponseTimeoutSeconds and the session apologized and hung up (it came back after the apology, and the history
    /// showed it as said).
    /// </remarks>
    private string MarkWhatTheCallerCutOff(string content)
    {
        var final = content?.Trim() ?? string.Empty;
        string sent;
        string? heard;
        lock (_sync)
        {
            if (_superseded)
            {
                return final.Length == 0
                    ? "[the caller went on talking before anything was said]"
                    : $"[the caller went on talking before this was said; they did not hear: \"{final}\"]";
            }
            // Nothing of it went out before the call ended, and nothing is said once it has (SpeakAsync)
            if (_ended && !_streamedThisTurn && final.Length > 0)
            {
                return $"[the call ended before this was said; the caller did not hear: \"{final}\"]";
            }
            if (!_cutInMidReply || final.Length == 0)
            {
                return content;
            }
            sent = (_utteranceOpen ? _streamSegment.ToString() : _lastStreamSegment).Trim();
            heard = _heardBeforeCutIn?.Trim();
        }

        // The reply is not what was streamed (text set by a tool, or a reply rewritten after the model wrote it),
        // so where the caller cut in is not known
        if (sent.Length == 0 || !final.StartsWith(sent, StringComparison.Ordinal))
        {
            return content;
        }

        // Empty when the caller cut in before any of it played: then none of it was heard
        if (heard != null && sent.StartsWith(heard, StringComparison.Ordinal))
        {
            sent = heard;
        }
        else
        {
            _logger.LogInformation($"Where playback stopped in conversation {_conversationId} doesn't match the " +
                "reply that went out; marking the cut where the sending stopped.");
        }

        var unheard = final.Substring(sent.Length).Trim();
        return unheard.Length == 0 ? content : ConversationRelayCutIn.Mark(sent, unheard);
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
    /// A tool is about to run: from now on the turn has done something that stays done
    /// </summary>
    private void MarkToolCalled()
    {
        lock (_sync)
        {
            _toolCalledThisTurn = true;
        }
    }

    private async Task SpeakToolIndicationAsync(string indication)
    {
        MarkToolCalled();
        lock (_sync)
        {
            // A tool the model asked for just before the turn was cancelled still runs; it is not announced
            if (_superseded)
            {
                return;
            }
        }
        await SpeakIndicationAsync(indication);
    }

    private async Task CloseUtteranceBeforeToolAsync()
    {
        MarkToolCalled();
        await CloseUtteranceAsync();
    }

    /// <summary>
    /// Speaks a tool-call indication: "#name" is a prerecorded prompt, anything else is spoken as is;
    /// segments are separated by "|". Said while the flow keeps what the agent says uninterrupted, the spoken text plays
    /// to the end too.
    /// </summary>
    /// <remarks>
    /// Why: the recap said while the request went in, and the line before it, were each cut off by an interrupt 0 ms
    /// into them with no words after it, so the caller heard neither (dev call 4bcd88e1); the reply after them, kept
    /// uninterrupted, played whole.
    /// </remarks>
    private async Task SpeakIndicationAsync(string indication)
    {
        await CloseUtteranceAsync();
        var keep = ReadKeepUninterrupted();
        foreach (var segment in indication.Split('|'))
        {
            var text = segment.Trim();
            if (text.StartsWith('#'))
            {
                await SpeakPromptAsync(text.Substring(1), fallbackText: null);
            }
            else
            {
                await SpeakAsync(text, keepUninterrupted: keep);
            }
        }
    }

    /// <summary>
    /// Speaks a prerecorded prompt's configured text so the voice matches; without one, plays the recording.
    /// </summary>
    /// <param name="canSpeak">Extra condition, checked under <see cref="_sync"/> once the send lock is held</param>
    private async Task SpeakPromptAsync(string name, string? fallbackText, Func<bool>? canSpeak = null)
    {
        if (_settings.PromptTexts.TryGetValue(name, out var text) && !string.IsNullOrWhiteSpace(text))
        {
            await SpeakAsync(text, canSpeak);
        }
        else if (fallbackText != null)
        {
            await SpeakAsync(fallbackText, canSpeak);
        }
        else
        {
            await SpeakIfAsync(() => !_ended && (canSpeak?.Invoke() ?? true),
                new { type = "play", source = $"{_settings.CallbackHost}/twilio/{name}.mp3" }, TimeSpan.FromSeconds(3));
        }
    }

    /// <param name="canSpeak">Extra condition, checked under <see cref="_sync"/> once the send lock is held</param>
    /// <param name="keepUninterrupted">The caller must hear all of it, so it plays to the end whoever speaks over it</param>
    /// <param name="sayAgainIfStuck">Said again on a new session if it never finished playing (RememberUtterance)</param>
    /// <returns>Whether it went out</returns>
    private async Task<bool> SpeakAsync(string? text, Func<bool>? canSpeak = null, bool keepUninterrupted = false,
        bool sayAgainIfStuck = true)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return await SpeakIfAsync(() =>
            {
                if (_ended || !(canSpeak?.Invoke() ?? true))
                {
                    return false;
                }
                RememberUtterance(text, _saidCount + 1, keepUninterrupted, sayAgainIfStuck);
                return true;
            },
            TextFrame(text, last: true, keepUninterrupted), EstimateSpeech(text.Length));
    }

    /// <summary>
    /// Sends something the caller hears, if <paramref name="canSpeak"/> still holds once the send lock is held. How long
    /// it takes to say is recorded before the frame goes out, so a cut-in arriving once it plays restarts the silence
    /// clock instead of being overwritten by it.
    /// </summary>
    private async Task<bool> SpeakIfAsync(Func<bool> canSpeak, object payload, TimeSpan duration)
    {
        return await SendIfAsync(() =>
        {
            if (!canSpeak())
            {
                return false;
            }
            MarkSpeaking(duration);
            return true;
        }, payload);
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

    private record PendingInput(string Content, string? Digits, DateTime ReceivedUtc);

    /// <summary>
    /// Whether an interrupt landed on the last line said after its turn. Call under <see cref="_sync"/>.
    /// </summary>
    /// <remarks>
    /// Twilio's utteranceUntilInterrupt is what it had played of the utterance it was on: when it is the start of the
    /// line, the caller cut into the line, whatever was queued behind it; when it is the whole line, the caller heard
    /// all of it. Otherwise (missing, empty, or another utterance's words, such as a filler or a "say it again" prompt)
    /// it is the line only if nothing was said after it, since only something playing can be interrupted. Words of
    /// something said before the line mean the line was still queued behind it: cut off before any of it played.
    /// </remarks>
    private bool CutIntoAfterTurnLine(string? heard)
    {
        var playing = heard?.Trim();
        var line = _afterTurnLine!.Trim();
        if (!string.IsNullOrEmpty(playing) && line.StartsWith(playing, StringComparison.Ordinal))
        {
            return playing.Length < line.Length;
        }
        return _saidCount == _afterTurnLineMark || PlayingBeforeAfterTurnLine(playing);
    }

    /// <summary>
    /// What the caller heard of the last line said after its turn, when they cut into it: Twilio's utteranceUntilInterrupt
    /// when it is the start of the line, "" when none of it played (Twilio was still playing something said before it),
    /// otherwise null, as it is then not known. Call under <see cref="_sync"/>.
    /// </summary>
    private string? HeardOfAfterTurnLine(string? heard)
    {
        var playing = heard?.Trim();
        if (playing == null)
        {
            return null;
        }
        if (_afterTurnLine!.Trim().StartsWith(playing, StringComparison.Ordinal))
        {
            return playing;
        }
        return PlayingBeforeAfterTurnLine(playing) ? string.Empty : null;
    }

    // Twilio was playing something said before the after-turn line (a filler, the reply's first part) when the caller cut
    // in. Why: a line queued behind a filler that was cut off was taken as heard, and the caller's "Yes." to the filler
    // as the answer to it (seen on calls). Only the latest thing said with those words counts: a filler said again on a
    // later turn ("One moment, please.") is the one playing, not the same words said before the line.
    private bool PlayingBeforeAfterTurnLine(string? playing)
    {
        if (string.IsNullOrEmpty(playing) || _afterTurnLine!.Trim().StartsWith(playing, StringComparison.Ordinal))
        {
            return false;
        }
        var latest = _utterances.LastOrDefault(u => u.Text.StartsWith(playing, StringComparison.Ordinal));
        return latest.Text != null && latest.Mark < _afterTurnLineMark;
    }

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
    /// Sends only if <paramref name="canSend"/> still holds once the send lock is held, so a decision taken
    /// before waiting for the lock cannot go stale. It runs under <see cref="_sync"/> and may update session
    /// state that must change only when the send goes ahead.
    /// </summary>
    private async Task<bool> SendIfAsync(Func<bool> canSend, object payload)
    {
        await _sendLock.WaitAsync();
        try
        {
            lock (_sync)
            {
                if (!canSend())
                {
                    return false;
                }
            }
            await SendLockedAsync(payload);
            return true;
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

    /// <summary>
    /// Moves the start of caller silence past something about to be said. Call under <see cref="_sync"/>, before the
    /// frame is sent.
    /// </summary>
    private void MarkSpeaking(TimeSpan duration)
    {
        _spokeThisTurn = true;
        _saidCount++;
        var now = DateTime.UtcNow;
        var start = _quietSinceUtc == DateTime.MaxValue || _quietSinceUtc < now ? now : _quietSinceUtc;
        _quietSinceUtc = start + duration;
        // Queued behind what is still to play; unlike the silence clock, the caller's words don't move it
        _playedByUtc = (_playedByUtc < now ? now : _playedByUtc) + duration;
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
