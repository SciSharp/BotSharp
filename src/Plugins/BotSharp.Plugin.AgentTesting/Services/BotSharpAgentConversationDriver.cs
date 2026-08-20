using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.AgentTesting.Runtime;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The layer that talks to a real BotSharp conversation. Deliberately untested by unit tests --
/// testing it would amount to testing BotSharp itself, and its correctness is established by the
/// end-to-end smoke run instead. Before changing anything here, check the real signatures in the
/// BotSharp source: they have changed between versions, and this implementation was written against
/// the sibling worktree's source rather than from documentation.
///
/// How `ct` is handled: neither SendMessage nor InvokeFunction takes a CancellationToken, and
/// BotSharp's internal routing/tool-invocation loop ignores cancellation entirely. So this only
/// fails fast with a single ct.ThrowIfCancellationRequested() at method entry, before any real call
/// has gone out and while there is therefore no orphan risk. It must never wrap the returned Task
/// in .WaitAsync(ct): that would make "the caller stopped waiting" look identical to "the call
/// actually stopped", and AgentTestCaseRunner needs the raw Task itself so that on a timeout it can
/// keep it running in the background and only remove the registry entry once it truly finishes --
/// otherwise the mock seam vanishes while an orphaned call is still running and its next tool call
/// lands on the real implementation. Anyone about to add a .WaitAsync to these two methods should
/// read this paragraph again first.
/// </summary>
public class BotSharpAgentConversationDriver : IAgentConversationDriver
{
    private readonly IConversationService _conversations;
    private readonly IRoutingService _routing;
    private readonly IBotSharpRepository _repository;
    private readonly IAgentService _agents;
    private readonly ILogger<BotSharpAgentConversationDriver> _logger;

    // Set at most once per instance: this driver is scoped per-case (a fresh DI scope per case,
    // see AgentTestRunQueue.ScopedCaseRunner), so exactly one conversation ever passes through it.
    private bool _conversationTagged;

    public BotSharpAgentConversationDriver(
        IConversationService conversations,
        IRoutingService routing,
        IBotSharpRepository repository,
        IAgentService agents,
        ILogger<BotSharpAgentConversationDriver> logger)
    {
        _conversations = conversations;
        _routing = routing;
        _repository = repository;
        _agents = agents;
        _logger = logger;
    }

    public async Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates)
    {
        // SendMessage (called later, per turn) is what actually creates the conversation row via
        // GetConversationRecordOrCreateNew(agentId) -- this step only has to bind the ambient
        // conversation id and seed its initial states before that happens.
        //
        // Deliberately does NOT seed a "channel" state. An earlier version of this method did,
        // to stamp Conversation.Channel as synthetic before the row was created -- but "channel"
        // is CustomStateKeys.Channel, a load-bearing PRODUCTION state key with ~30 readers across
        // this codebase (IntelligentDiagnosisRoutingHook.OnRoutingRulesLoaded branches on it
        // against ConversationChannel.OpenAPI before routing rules even load; several Functions
        // gate visibility/behavior on it too), none of which recognize "agent-test" as a value --
        // forcing it changed real routing/business-logic branches mid-case. It also silently
        // corrupted recording: AgentTestRecorder.BuildDraft copies every seeded (MessageId == null)
        // state into InitialStates, so a recorded case's OWN real channel was overwritten by this
        // marker on every recording, and replaying that case then ran a DIFFERENT code path than
        // the one actually recorded. The "test-set" conversation tag (EnsureConversationTaggedAsync
        // below) already satisfies "make a harness conversation identifiable after the fact" on its
        // own -- see AgentTestConversationMarker's remaining doc comment. Do not reintroduce a
        // channel/other state-key marker without first grepping every reader of that key.
        var states = initialStates
            .Select(s => new MessageState(s.Key, s.Value, s.ActiveRounds, s.Global))
            .ToList();

        await _conversations.SetConversationId(conversationId, states);
    }

    public async Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct)
    {
        // Fail fast only if the case had already timed out before we ever got here (e.g. a prior
        // turn ran long). Once the real call below starts, there is no cancellation hook left to
        // reach -- see the class-level note.
        ct.ThrowIfCancellationRequested();

        RoleDialogModel? last = null;

        await _conversations.SendMessage(
            agentId,
            new RoleDialogModel(AgentRole.User, userMessage),
            replyMessage: null,
            onResponseReceived: r =>
            {
                last = r;
                return Task.CompletedTask;
            });

        // The conversation row is only guaranteed to exist once the first SendMessage call above
        // has returned (see PrepareAsync's note) -- tagging any earlier would either no-op against
        // a row that doesn't exist yet or race its creation. Tagging is best-effort: a failure here
        // must never fail the case itself, since the case's own assertions -- not a housekeeping
        // label -- are what decide pass/fail.
        await EnsureConversationTaggedAsync(conversationId);

        return last?.RichContent?.Message?.Text ?? last?.Content;
    }

    private async Task EnsureConversationTaggedAsync(string conversationId)
    {
        if (_conversationTagged)
        {
            return;
        }

        _conversationTagged = true;
        try
        {
            await _repository.AppendConversationTags(conversationId, [AgentTestConversationMarker.Tag]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to tag agent test conversation {ConversationId} with {Tag}; the case's own "
                + "result is unaffected, but this conversation will not be reverse-traceable as synthetic.",
                conversationId, AgentTestConversationMarker.Tag);
        }
    }

    public async Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var message = new RoleDialogModel(AgentRole.Assistant, string.Empty)
        {
            FunctionName = AgentTestCanary.FunctionName,
            CurrentAgentId = agentId
        };

        // The bool RoutingService.InvokeFunction returns is not the signal we want here: it is
        // whether some executor ran, not whether OUR mock seam was the one that ran it. The seam
        // is only proven live by the content it stamps onto the message -- see MockFunctionExecutor.
        await _routing.InvokeFunction(AgentTestCanary.FunctionName, message);

        return message.Content == AgentTestCanary.ExpectedContent;
    }

    public Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId)
    {
        // Fix round 1, Finding 2: MockFunctionExecutor's StateWrites (and PrepareAsync's
        // InitialStates) go through IConversationStateService.SetState, which only mutates the
        // in-memory _curStates dictionary. The single writer of the PERSISTED store --
        // ConversationStateService.Save() -> IBotSharpRepository.UpdateConversationStates -- is
        // never called anywhere on the SendMessage/InstructDirect/InstructLoop/InvokeFunction call
        // chain this driver drives (confirmed by reading every caller of Save()/
        // UpdateConversationStates in both BotSharp.Core and the Mongo storage plugin: it is
        // transport middleware, controllers, the rule engine, and onebrain's own hooks/functions --
        // none of which run here). So reading via IBotSharpRepository.GetConversationStates (the
        // original implementation) would see a brand-new conversation's states as permanently
        // empty, and every stateEquals assertion would report "state 'X' is not set" against a
        // correctly-behaving agent.
        //
        // IConversationService.States is the SAME IConversationStateService instance
        // (ConversationService.States => _state, constructor-injected) that TestMockExecutorProvider
        // handed to MockFunctionExecutor -- both come out of the one DI scope this whole
        // conversation turn runs in, so GetStates() observes those writes directly, with no
        // persistence round-trip and no risk of a silent no-op (ConversationStateService.Save()
        // early-returns under some conditions -- e.g. sidecar mode -- that reading live state
        // sidesteps entirely). GetStates() also correctly excludes values whose ActiveRounds window
        // has already expired (StateValue.Active == false), which the old repository-based read did
        // not.
        var states = _conversations.States.GetStates();

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in states)
        {
            result[pair.Key] = pair.Value;
        }

        return Task.FromResult<IReadOnlyDictionary<string, string?>>(result);
    }

    public async Task<string?> ReadRoutedAgentNameAsync(string conversationId)
    {
        var dialogs = await _repository.GetConversationDialogs(conversationId);
        var lastAssistantDialog = dialogs.LastOrDefault(d => d.MetaData?.Role == AgentRole.Assistant);

        var agentId = lastAssistantDialog?.MetaData?.AgentId;
        if (string.IsNullOrEmpty(agentId))
        {
            return null;
        }

        var agent = await _agents.GetAgent(agentId);
        return agent?.Name;
    }
}
