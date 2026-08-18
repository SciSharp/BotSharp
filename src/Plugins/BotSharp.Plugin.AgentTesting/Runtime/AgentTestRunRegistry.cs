using System.Collections.Concurrent;

namespace BotSharp.Plugin.AgentTesting.Runtime;

/// <summary>
/// One test case currently executing. Indexed by conversationId because the test context has to be
/// reliable across threads: AsyncLocal is silently lost across a background-queue or SideCar
/// boundary, and losing it means real tools get executed.
/// </summary>
public class ActiveTestRun
{
    public string ConversationId { get; set; } = default!;
    public string CaseId { get; set; } = default!;

    /// <summary>
    /// The model this execution is forced onto; null = no override, use the agent's own LlmConfig.
    ///
    /// Applied by AgentTestModelOverrideHook, writing into agent.LlmConfig at the moment the agent
    /// finishes loading. That moment is the only one that works: the main conversation path,
    /// RoutingService.InvokeAgent, passes agent.LlmConfig.Provider/Model to CompletionProvider
    /// EXPLICITLY, and CompletionProvider.GetProviderAndModel only consults the conversation-state
    /// override when what it was passed is empty. So writing provider/model into conversation state
    /// has no effect on the main path, and overriding any later than agent load is already too late.
    /// </summary>
    public TestModel? ModelOverride { get; set; }

    public IReadOnlyList<TestToolMock> Mocks { get; set; } = [];
    public string UnmockedToolPolicy { get; set; } = UnmockedToolPolicies.Block;
    public ISet<string> AllowedFunctions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ISet<string> ForceBlockedFunctions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Which turn is currently running, so tool calls can be attributed to a turn.</summary>
    public int CurrentTurnIndex { get; set; }

    /// <summary>
    /// Whether the canary was intercepted. At run time the verdict does NOT come from this flag but
    /// from the content the canary call returns (see AgentTestCaseRunner); the flag exists so a test
    /// can assert directly that MockFunctionExecutor recognises the canary function name.
    /// </summary>
    public bool CanaryIntercepted { get; set; }

    private readonly List<ObservedToolCall> _observed = [];
    private readonly ConcurrentDictionary<string, int> _ordinals = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ObservedToolCall> ObservedCalls
    {
        get { lock (_observed) return _observed.ToList(); }
    }

    public void Record(ObservedToolCall call)
    {
        lock (_observed) _observed.Add(call);
    }

    /// <summary>
    /// Which call this is for a given function name (0-based), for matching TestToolMock.CallIndex.
    /// </summary>
    public int NextCallOrdinal(string functionName)
        => _ordinals.AddOrUpdate(functionName, 0, (_, prev) => prev + 1);
}

public interface IAgentTestRunRegistry
{
    void Register(ActiveTestRun run);
    void Unregister(string conversationId);
    ActiveTestRun? TryGet(string? conversationId);
}

public class AgentTestRunRegistry : IAgentTestRunRegistry
{
    private readonly ConcurrentDictionary<string, ActiveTestRun> _runs = new();

    public void Register(ActiveTestRun run) => _runs[run.ConversationId] = run;

    public void Unregister(string conversationId) => _runs.TryRemove(conversationId, out _);

    public ActiveTestRun? TryGet(string? conversationId)
        => string.IsNullOrEmpty(conversationId) ? null
           : _runs.TryGetValue(conversationId, out var run) ? run : null;
}

/// <summary>
/// Control-flow functions allowed through by default. **Do not turn this into a `util-` prefix
/// match**: `util-email-handle_email_sender`, `util-twilio-outbound_phone_call`,
/// `util-twilio-text_message`, `util-http-handle_http_request` and `util-db-sql_select` all start
/// with `util-` and all have real side effects. Allowing by prefix means one test run really sends
/// the emails and really places the calls.
/// </summary>
public static class ControlFlowFunctions
{
    public static readonly IReadOnlySet<string> Default = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "route_to_agent",
        "response_to_user",
        "human_intervention_needed",
        "util-routing-fallback_to_router",
        "util-instruct-execute_template"
    };
}

public static class AgentTestCanary
{
    /// <summary>
    /// The runner calls this function name once before starting, to prove the seam is actually
    /// live. If BotSharp.Core resolves to an older package without IFunctionExecutorProvider
    /// support, mocking fails silently -- the canary turns that into an explicit failure.
    /// </summary>
    public const string FunctionName = "__agent_test_canary__";

    /// <summary>
    /// The one sentinel value written by the interceptor (MockFunctionExecutor) and compared by the
    /// verifier (BotSharpAgentConversationDriver). Keeping a bare "canary" literal in both places
    /// used to be a hazard in this safety-critical check -- drifting apart would only ever make
    /// every case report Error rather than pass silently, but there should still be exactly one
    /// definition.
    /// </summary>
    public const string ExpectedContent = "canary";
}

/// <summary>
/// Marks every conversation this harness creates as synthetic, so an operator who stumbles on a
/// strange conversation in the admin UI (or an analytics job aggregating over the collection) can
/// tell it apart from a genuine customer conversation. AgentTestCaseResult.ConversationId already
/// gives forward traceability (result -> conversation); this is the reverse direction.
///
/// Deliberately just ONE marker (the tag), not a Channel/state-key marker too -- a prior version
/// of this class also declared a Channel constant seeded as the "channel" conversation state, but
/// that key (CustomStateKeys.Channel) is load-bearing production state with ~30 readers across
/// this codebase (routing hooks, visibility gates, etc.), none of which recognize a synthetic
/// value, and AgentTestRecorder.BuildDraft copies every seeded state into a recorded case's own
/// InitialStates -- so it also silently overwrote a recorded case's REAL channel on every
/// recording. The tag alone already satisfies the "make it identifiable after the fact" goal
/// without touching anything BotSharp/onebrain branches on. Do not add another state-key marker
/// without grepping every reader of that key first.
/// </summary>
public static class AgentTestConversationMarker
{
    /// <summary>
    /// The SAME tag value BotSharp-UI's chat window already writes when a human manually tags a
    /// real conversation as a test conversation (ConversationTag.Test in the UI's
    /// src/lib/helpers/enums.js) -- reusing it means any current or future code that filters this
    /// tag out of normal conversation views/analytics already covers harness-created conversations
    /// too, with nothing new to teach it.
    /// </summary>
    public const string Tag = "test-set";
}
