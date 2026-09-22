using BotSharp.Abstraction.Realtime.Options;
using BotSharp.Abstraction.Realtime.Settings;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

/// <summary>
/// Full duplex voice provider for gpt-live-1, served from wss://api.openai.com/v1/live/sessions.
/// Reference to https://developers.openai.com/api/docs/guides/live
///
/// Live differs from the realtime endpoint in three ways that shape this implementation:
/// the model listens and speaks at the same time and arbitrates interruptions itself,
/// transcripts arrive as deltas with no turn completion marker, and reasoning plus tool
/// calls are delegated to a separate backend model.
/// </summary>
public partial class LiveCompletionProvider : ILiveCompletion
{
    public string Provider => "openai";
    public string Model => _model;

    private readonly IServiceProvider _services;
    private readonly ILogger<LiveCompletionProvider> _logger;
    private readonly BotSharpOptions _botsharpOptions;
    private readonly OpenAiSettings _openAiSettings;

    /// <summary>
    /// Level for the event tracing below. A local debug build raises it to Critical so the live
    /// traffic stands out in the console; anywhere else it stays at Information, where a running
    /// call does not read as a fault.
    /// </summary>
#if DEBUG
    private const LogLevel TraceLevel = LogLevel.Critical;
#else
    private const LogLevel TraceLevel = LogLevel.Information;
#endif

    private string _model = LiveModelConstants.GPT_Live_1;
    private LlmRealtimeSession? _session;
    private RealtimeOptions? _realtimeOptions;
    private bool _isBlocking = false;

    /// <summary>
    /// Last delegation handed out by the model, used to attribute appended context.
    /// </summary>
    private string? _currentDelegationId;

    /// <summary>
    /// Agent instruction most recently sent to the backend handler. Callers hand it back to
    /// TriggerModelInference to mean "carry on", which must not be appended to the voice model.
    /// </summary>
    private string? _lastAgentInstruction;

    /// <summary>
    /// Provider and model of the backend handler, as resolved for the agent when the session was
    /// last configured. Kept so the token stats can be filed against the model that actually
    /// spent them, from the response event that reports the usage - which carries no agent.
    /// </summary>
    private string? _backendProvider;
    private string? _backendModel;

    private RealtimeHubConnection _conn = null!;
    private Func<Task> _onModelReady = null!;
    private Func<string, string, Task> _onModelAudioDeltaReceived = null!;
    private Func<Task> _onModelAudioResponseDone = null!;
    private Func<string, Task> _onModelAudioTranscriptDone = null!;
    private Func<List<RoleDialogModel>, Task> _onModelResponseDone = null!;
    private Func<string, Task> _onConversationItemCreated = null!;
    private Func<RoleDialogModel, Task> _onInputAudioTranscriptionDone = null!;
    private Func<Task> _onInterruptionDetected = null!;

    public LiveCompletionProvider(
        IServiceProvider services,
        ILogger<LiveCompletionProvider> logger,
        BotSharpOptions botsharpOptions,
        OpenAiSettings openAiSettings)
    {
        _services = services;
        _logger = logger;
        _botsharpOptions = botsharpOptions;
        _openAiSettings = openAiSettings;
    }

    private LiveSettings LiveSettings => _openAiSettings.Live ?? new LiveSettings();

    public async Task Connect(
        RealtimeHubConnection conn,
        Func<Task> onModelReady,
        Func<string, string, Task> onModelAudioDeltaReceived,
        Func<Task> onModelAudioResponseDone,
        Func<string, Task> onModelAudioTranscriptDone,
        Func<List<RoleDialogModel>, Task> onModelResponseDone,
        Func<string, Task> onConversationItemCreated,
        Func<RoleDialogModel, Task> onInputAudioTranscriptionDone,
        Func<Task> onInterruptionDetected)
    {
        _logger.LogInformation($"Connecting {Provider} live server...");

        _conn = conn;
        _onModelReady = onModelReady;
        _onModelAudioDeltaReceived = onModelAudioDeltaReceived;
        _onModelAudioResponseDone = onModelAudioResponseDone;
        _onModelAudioTranscriptDone = onModelAudioTranscriptDone;
        _onModelResponseDone = onModelResponseDone;
        _onConversationItemCreated = onConversationItemCreated;
        _onInputAudioTranscriptionDone = onInputAudioTranscriptionDone;
        _onInterruptionDetected = onInterruptionDetected;

        _currentDelegationId = null;
        _lastAgentInstruction = null;
        _backendProvider = null;
        _backendModel = null;
        ResetSessionState();

        var realtimeSettings = _services.GetRequiredService<RealtimeModelSettings>();

        _session = new LlmRealtimeSession(_services, new ChatSessionOptions
        {
            Provider = Provider,
            JsonOptions = _botsharpOptions.JsonSerializerOptions,
            Logger = _logger
        });

        var modelSetting = GetModelSetting();

        // The Live endpoint carries the model in session.start rather than in the query string.
        await _session.ConnectAsync(
            uri: GetEndpoint(modelSetting),
            headers: BuildHeaders(modelSetting),
            cancellationToken: CancellationToken.None);

        // session.start must be the first message on the socket.
        var (sessionConfig, _) = await BuildSessionConfig(conn, isInit: true);
        var startEvent = new
        {
            type = LiveClientEventType.SessionStart,
            event_id = NewEventId("start"),
            session = sessionConfig
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // An unknown field makes the server reject the whole session, so log what was sent.
            _logger.LogDebug("{Type}: {Payload}", LiveClientEventType.SessionStart,
                JsonSerializer.Serialize(startEvent, _botsharpOptions.JsonSerializerOptions));
        }

        await SendEventToModel(startEvent);

        _ = ReceiveMessage(realtimeSettings);
    }

    public async Task Reconnect(RealtimeHubConnection conn)
    {
        _logger.LogInformation($"Reconnecting {Provider} live server...");

        _isBlocking = true;
        _conn = conn;
        await Disconnect();
        await Task.Delay(500);
        await Connect(
            _conn,
            _onModelReady,
            _onModelAudioDeltaReceived,
            _onModelAudioResponseDone,
            _onModelAudioTranscriptDone,
            _onModelResponseDone,
            _onConversationItemCreated,
            _onInputAudioTranscriptionDone,
            _onInterruptionDetected);
    }

    public async Task Disconnect()
    {
        _logger.LogInformation($"Disconnecting {Provider} live server...");

        // The conversation is ending, so record the turn nobody spoke after. Done before the
        // timers go, since disposing them would drop the only other route to that last turn.
        await FlushPendingTurns();

        DisposeSessionWorkers();

        if (_session != null)
        {
            // Asking the server to close yields a final session.closed carrying billed duration.
            await SendEventToModel(new
            {
                type = LiveClientEventType.SessionClose,
                event_id = NewEventId("close")
            });

            await _session.DisconnectAsync();
            _session.Dispose();
            _session = null;
        }
    }

    public async Task AppenAudioBuffer(string message)
    {
        if (_isBlocking) return;

        await SendEventToModel(new
        {
            type = LiveClientEventType.InputAudioAppend,
            audio = message
        });
    }

    public async Task AppenAudioBuffer(ArraySegment<byte> data, int length)
    {
        if (_isBlocking) return;

        var message = Convert.ToBase64String(data.AsSpan(0, length).ToArray());
        await AppenAudioBuffer(message);
    }

    public async Task SendEventToModel(object message)
    {
        if (_session == null) return;

        await _session.SendEventToModelAsync(message);
    }

    /// <summary>
    /// Live has no manual turn taking: the model decides when to speak. Text handed in here is
    /// something to say now, so it goes to the channel the model speaks from rather than to
    /// session.instructions, which would make a one-off line a permanent standing directive.
    /// </summary>
    public async Task TriggerModelInference(string? instructions = null)
    {
        // RealtimeConversationHook passes the agent instruction back purely to mean "continue".
        // Speaking that aloud would read the whole agent prompt to the caller.
        if (!string.IsNullOrWhiteSpace(instructions)
            && !string.Equals(instructions, _lastAgentInstruction, StringComparison.Ordinal))
        {
            await AppendSessionContext(LiveClientEventType.CommentaryAppend, ExtractSpokenText(instructions));
        }

        await SendEventToModel(new
        {
            type = LiveClientEventType.ResponseCreate,
            event_id = NewEventId("continue")
        });
    }

    /// <summary>
    /// Live arbitrates barge-in inside the model, so there is no response to cancel.
    /// </summary>
    public Task CancelModelResponse()
    {
        _logger.LogDebug("{Provider} handles interruption in-model; cancel request ignored.", Provider);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The live conversation history is owned by the server and cannot be edited item by item.
    /// </summary>
    public Task RemoveConversationItem(string itemId)
    {
        _logger.LogWarning("{Provider} does not support removing conversation item {ItemId}.", Provider, itemId);
        return Task.CompletedTask;
    }

    public async Task InsertConversationItem(RoleDialogModel message)
    {
        if (message.Role == AgentRole.Function)
        {
            // Tool results go back to the backend handler, not to the voice model.
            await SendEventToModel(new
            {
                type = LiveClientEventType.ResponseItemCreate,
                event_id = NewEventId("tool_result"),
                item = new
                {
                    type = "function_call_output",
                    call_id = message.ToolCallId,
                    output = message.Content ?? string.Empty
                }
            });
        }
        else if (message.Role == AgentRole.Assistant)
        {
            // Commentary is content the model paraphrases and speaks aloud.
            await AppendSessionContext(LiveClientEventType.CommentaryAppend, message.Content);
        }
        else if (message.Role == AgentRole.User)
        {
            await SendEventToModel(new
            {
                type = LiveClientEventType.ResponseItemCreate,
                event_id = NewEventId("typed_input"),
                item = new
                {
                    type = "message",
                    role = AgentRole.User,
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = message.Content ?? string.Empty
                        }
                    }
                }
            });
        }
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public void SetOptions(RealtimeOptions? options)
    {
        _realtimeOptions = options;
    }

    #region Private methods
    /// <summary>
    /// Callers wrap what they want said, as in: Say to user: "your order shipped". Commentary is
    /// paraphrased aloud, so the wrapper has to come off or the model reads the instruction out.
    /// Anything that is not a short prefix followed by a fully quoted line is passed through.
    /// </summary>
    private static string ExtractSpokenText(string instructions)
    {
        var match = QuotedPayload.Match(instructions.Trim());
        return match.Success ? match.Groups[1].Value : instructions;
    }

    private static readonly Regex QuotedPayload =
        new(@"^[^""]{0,40}:\s*""(.+)""$", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Sends one of the session.*.append events. Each append is capped at 500 tokens
    /// server side, so the content is truncated before it leaves.
    /// </summary>
    private async Task AppendSessionContext(string eventType, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        // Instructions are session wide directives, so they are not attributed to a delegation.
        // Thinking and commentary answer a specific delegation the model handed out.
        var delegationId = eventType == LiveClientEventType.InstructionsAppend
            ? null
            : _currentDelegationId;

        // A misconfigured budget would otherwise drop the content entirely.
        var maxTokens = Math.Max(16, LiveSettings.MaxAppendTokens);
        // Appends concatenate on the server, so splitting long content across several of them
        // reassembles the original rather than corrupting it. Only the chunk ceiling loses text.
        var (chunks, truncated) = LiveAppendBudget.SplitIntoChunks(content, maxTokens, LiveAppendBudget.MaxChunks);

        if (truncated)
        {
            var estimated = LiveAppendBudget.EstimateTokens(content);

            if (eventType == LiveClientEventType.InstructionsAppend)
            {
                // Half a directive can mean the opposite of the whole - "do not mention the
                // discount to the user" cut short still reads as an instruction to obey. Leaving
                // the model on its existing instructions is the safer failure.
                _logger.LogError(
                    "Dropping a {Tokens} token instruction: it exceeds {Chunks} appends of {Max} tokens and " +
                    "cannot be delivered without risking a partial directive.",
                    estimated, LiveAppendBudget.MaxChunks, maxTokens);
                return;
            }

            _logger.LogWarning(
                "{EventType} content of about {Tokens} tokens exceeded {Chunks} appends of {Max} tokens and was trimmed.",
                eventType, estimated, LiveAppendBudget.MaxChunks, maxTokens);

            if (chunks.Count > 0)
            {
                chunks[^1] += LiveAppendBudget.ElisionMarker;
            }
        }

        foreach (var chunk in chunks)
        {
            await SendEventToModel(new
            {
                type = eventType,
                event_id = NewEventId("ctx"),
                delegation_id = delegationId,
                content = chunk
            });
        }
    }

    private Dictionary<string, string> BuildHeaders(LlmModelSetting? settings)
    {
        return new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {settings?.ApiKey}" }
        };
    }

    /// <summary>
    /// Socket address from the model's entry in LlmProviders, so a deployment can point the call
    /// at a proxy or a regional host without a code change, falling back to the public Live
    /// endpoint. A configured value that is not a usable absolute URI is logged and ignored
    /// rather than thrown: a typo in settings should not take the call down.
    /// </summary>
    private Uri GetEndpoint(LlmModelSetting? settings)
    {
        var endpoint = settings?.Endpoint;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new Uri(LiveModelConstants.Endpoint);
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Ignoring the endpoint configured for {Provider}.{Model}, which is not an absolute URI: {Endpoint}.",
                Provider, _model, endpoint);
            return new Uri(LiveModelConstants.Endpoint);
        }

        return uri;
    }

    /// <summary>
    /// Looks up a model's settings under the OpenAI provider key, which is shared with the
    /// realtime and chat providers: the live models are listed alongside them in LlmProviders.
    /// </summary>
    private LlmModelSetting? GetModelSetting(string? model = null)
    {
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();
        model = model.IfNullOrEmptyAs(_model);

        return llmProviderService.GetSetting(Provider, model);
    }

    private static string NewEventId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
    #endregion
}
