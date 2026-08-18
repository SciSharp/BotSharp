using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Repositories;
using BotSharp.Plugin.AgentTesting.Repositories;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Records an editable draft case from a real BotSharp conversation. This is what decides whether
/// QA and PM can actually use this feature: hand-writing a work order agent's mock JSON is not
/// realistic.
///
/// <see cref="BuildDraft"/> is a pure function: the same (suiteId, conversationId, dialogs, states)
/// always yields the same <see cref="AgentTestCase"/> with no I/O (the logger is an optional
/// diagnostic outlet that affects logging only, never the return value), so it can be unit-tested
/// without Mongo or BotSharp. <see cref="LoadAndBuildAsync"/> is the thin layer that touches the
/// real data source: read <see cref="IBotSharpRepository"/>, map to
/// <see cref="RecordedDialog"/>/<see cref="RecordedState"/>, call <see cref="BuildDraft"/>, store.
///
/// Two deliberate limitations -- change the spec before "fixing" either of them here:
/// 1) state writes can only be extracted as a whole-turn delta, attached to that turn's last mock.
///    StateValueMongoElement carries only MessageId (which locates a turn) and Source (one of
///    external/application/user), never a function name, so splitting the delta across individual
///    mocks automatically is not possible;
/// 2) no output-text assertions (outputContains/outputRegex) and no llmJudge are generated. Using
///    the model's exact wording as a baseline is extremely brittle -- any rephrasing goes red, and
///    recording one case would mean hand-editing ten assertions. Only the two stable kinds,
///    toolCalled and stateEquals, are generated.
/// </summary>
public class AgentTestRecorder
{
    private readonly IBotSharpRepository _repository;
    private readonly IAgentTestRepository _testRepository;
    private readonly ILogger<AgentTestRecorder> _logger;

    /// <summary>
    /// Optional: when null this recorder only has the deterministic path (which is how the existing
    /// unit tests construct it).
    /// </summary>
    private readonly ICaseSegmenter? _segmenter;

    public AgentTestRecorder(
        IBotSharpRepository repository,
        IAgentTestRepository testRepository,
        ILogger<AgentTestRecorder> logger,
        ICaseSegmenter? segmenter = null)
    {
        _repository = repository;
        _testRepository = testRepository;
        _logger = logger;
        _segmenter = segmenter;
    }

    /// <summary>
    /// Reads the real conversation, builds the draft, stores it, and returns the new draft case
    /// (with its Id already assigned).
    /// </summary>
    public async Task<AgentTestCase> LoadAndBuildAsync(string suiteId, string conversationId)
    {
        var (dialogs, states) = await LoadAsync(conversationId);

        var draft = BuildDraft(suiteId, conversationId, dialogs, states, _logger);

        await _testRepository.UpsertCaseAsync(draft);
        return draft;
    }

    /// <summary>Reads the raw conversation data recording needs. Shared by both recording paths.</summary>
    private async Task<(List<RecordedDialog> Dialogs, List<RecordedState> States)> LoadAsync(string conversationId)
    {
        var dialogElements = await _repository.GetConversationDialogs(conversationId);
        var conversationStates = await _repository.GetConversationStates(conversationId);

        var dialogs = dialogElements
            .Select(d => new RecordedDialog
            {
                Role = d.MetaData?.Role ?? string.Empty,
                Content = d.Content,
                FunctionName = d.MetaData?.FunctionName,
                FunctionArgs = d.MetaData?.FunctionArgs,
                MessageId = d.MetaData?.MessageId
            })
            .ToList();

        // ConversationState is a ConcurrentDictionary<string, StateKeyValue> -- the dictionary
        // KEY and StateKeyValue.Key are the same thing by construction (see
        // BotSharp.Plugin.MongoStorage's StateMongoElement round trip); reading it off the value
        // rather than the pair's own Key is just slightly more defensive.
        var states = conversationStates
            .Select(pair => new RecordedState
            {
                Key = pair.Value.Key,
                Values = pair.Value.Values
                    .Select(v => new RecordedStateValue
                    {
                        MessageId = v.MessageId,
                        Data = v.Data,
                        ActiveRounds = v.ActiveRounds
                    })
                    .ToList()
            })
            .ToList();

        return (dialogs, states);
    }

    /// <summary>
    /// As above, but first asks <see cref="ICaseSegmenter"/> to split the conversation into one or
    /// more scenarios, producing one draft per scenario. A null <paramref name="model"/> calls no
    /// model at all and falls through to <see cref="LoadAndBuildAsync"/> (returning a single-element
    /// list) -- so "do not use AI" is not a degraded branch, it is literally the original path.
    /// </summary>
    public async Task<List<AgentTestCase>> LoadAndBuildManyAsync(
        string suiteId,
        string conversationId,
        TestModel? model,
        CancellationToken ct = default)
    {
        if (model == null || _segmenter == null)
        {
            return [await LoadAndBuildAsync(suiteId, conversationId)];
        }

        var (dialogs, states) = await LoadAsync(conversationId);

        var turns = ToSegmentableTurns(dialogs);
        if (turns.Count == 0)
        {
            // Nothing to segment and nothing to record; let the deterministic path produce the
            // same (empty-turn) draft it always would rather than special-casing it here.
            return [await LoadAndBuildAsync(suiteId, conversationId)];
        }

        var segments = await _segmenter.SegmentAsync(turns, model, ct);
        var drafts = BuildDrafts(suiteId, conversationId, dialogs, states, segments, _logger);

        foreach (var draft in drafts)
        {
            await _testRepository.UpsertCaseAsync(draft);
        }

        return drafts;
    }

    /// <summary>
    /// Pure function: turns already-loaded conversation data into one disabled draft case. The
    /// <paramref name="logger"/> is used only for the single edge case of "this turn's state delta
    /// has no mock to attach to"; omitting it (as the unit tests do) means no log output and changes
    /// nothing about the return value.
    /// </summary>
    public static AgentTestCase BuildDraft(
        string suiteId,
        string conversationId,
        IReadOnlyList<RecordedDialog> dialogs,
        IReadOnlyList<RecordedState> states,
        ILogger? logger = null)
    {
        var draft = new AgentTestCase
        {
            SuiteId = suiteId,
            Name = $"Recorded from {conversationId}",
            Enabled = false,                                   // Enabled only after a human reviews it
            SourceConversationId = conversationId,
            UnmockedToolPolicy = UnmockedToolPolicies.Block
        };

        // TestTurn itself has no slot for the BotSharp MessageId that opened it (see
        // Repository/Mongo/AgentTestCase.cs) -- tracked here only for the duration of this call,
        // to compute each turn's own state delta (step 4) below.
        var turnMessageIds = new Dictionary<int, string?>();

        // turn.Index -> every mock created while that turn was open, in dialog order. Used both
        // to attach a turn's state delta to its LAST mock (step 4) and to generate one toolCalled
        // assertion per mock (step 5a).
        var mocksByTurn = new Dictionary<int, List<TestToolMock>>();

        // FunctionName -> FunctionArgs of the nearest preceding assistant call for that function,
        // scoped to the CURRENTLY OPEN TURN ONLY -- cleared every time a new turn opens, per the
        // "nearest preceding call within the same turn" rule.
        //
        // OrdinalIgnoreCase, matching ToolMockMatcher.Match/ActiveTestRun's own call-ordinal
        // tracker (both compare function names case-insensitively): two differently-cased
        // recordings of the same real function (e.g. a dialog logged "Get_Work_Order" once and
        // "get_work_order" another time) must be tracked as ONE function here too, or the args/
        // ordinal correction below silently stops being unambiguous -- the recorder would see two
        // single-occurrence functions (each kept its own ArgsMatchJson, each CallIndex 0), while
        // ToolMockMatcher.Match sees one function called twice and could resolve either replay
        // call to either mock.
        var pendingArgsByFunction = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // FunctionName -> how many times it has been recorded as a "function" dialog so far.
        // Doubles as: (a) during the loop, the next CallIndex to assign for that function; (b)
        // after the loop, its final value is the TOTAL number of times that function was recorded
        // across the whole conversation, which is exactly what the args-omission correction below
        // needs. OrdinalIgnoreCase for the same reason as pendingArgsByFunction above.
        var callCountByFunction = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        TestTurn? currentTurn = null;

        foreach (var dialog in dialogs)
        {
            if (dialog.Role == AgentRole.User)
            {
                currentTurn = new TestTurn
                {
                    Index = draft.Turns.Count,
                    UserMessage = dialog.Content ?? string.Empty
                };
                draft.Turns.Add(currentTurn);
                turnMessageIds[currentTurn.Index] = dialog.MessageId;
                pendingArgsByFunction.Clear();
                continue;
            }

            if (dialog.Role == AgentRole.Assistant && !string.IsNullOrEmpty(dialog.FunctionName))
            {
                pendingArgsByFunction[dialog.FunctionName] = dialog.FunctionArgs;
                continue;
            }

            if (dialog.Role == AgentRole.Function)
            {
                if (currentTurn == null)
                {
                    // A function result with no user turn open yet can't happen in a
                    // well-formed recording; there is nowhere sane to attach it, so it is
                    // skipped rather than fabricating a turn for it.
                    continue;
                }

                var functionName = dialog.FunctionName ?? string.Empty;
                var argsMatchJson = pendingArgsByFunction.TryGetValue(functionName, out var pendingArgs)
                    ? pendingArgs
                    : null;
                var callIndex = callCountByFunction.TryGetValue(functionName, out var count) ? count : 0;
                callCountByFunction[functionName] = callIndex + 1;

                var mock = new TestToolMock
                {
                    FunctionName = functionName,
                    ArgsMatchJson = argsMatchJson,
                    CallIndex = callIndex,
                    ResultContent = dialog.Content ?? string.Empty
                };

                draft.Mocks.Add(mock);

                if (!mocksByTurn.TryGetValue(currentTurn.Index, out var turnMocks))
                {
                    turnMocks = [];
                    mocksByTurn[currentTurn.Index] = turnMocks;
                }
                turnMocks.Add(mock);
            }
        }

        // Step 5a: one toolCalled assertion per mock, generated from each mock's CURRENTLY
        // recorded ArgsMatchJson -- deliberately BEFORE the args/ordinal correction below, not
        // after. Fix round 1: an earlier version of this method ran the correction first and
        // built assertions by reading mock.ArgsMatchJson back off the (by then corrected)
        // TestToolMock objects mocksByTurn holds references to -- so a repeated function's
        // assertion silently lost its argument check too, even though the correction is only
        // actually needed for ToolMockMatcher.Match, which dispatches against the WHOLE case's
        // mock list (MockFunctionExecutor.ExecuteAsync: Match(_run.Mocks, ...)) and is therefore
        // genuinely ambiguous across turns. AssertionEvaluator's toolCalled case, by contrast, is
        // evaluated per turn against ONLY that turn's observed calls
        // (AgentTestCaseRunner.cs: active.ObservedCalls.Where(c => c.TurnIndex == turn.Index)) --
        // there is no cross-turn collision for an assertion to guard against, so it must keep
        // whatever argument was actually recorded for that one call. TestAssertion.ArgsMatchJson
        // is a plain string copy at the moment this loop runs, so building assertions here and
        // nulling the MOCKS' own ArgsMatchJson afterward cannot un-set what was already copied.
        foreach (var turn in draft.Turns)
        {
            if (!mocksByTurn.TryGetValue(turn.Index, out var turnMocks))
            {
                continue;
            }

            foreach (var mock in turnMocks)
            {
                if (string.IsNullOrWhiteSpace(mock.FunctionName))
                {
                    // A Role=Function dialog with no FunctionName (malformed/incomplete recorded
                    // data) would otherwise produce a toolCalled assertion with a blank Target --
                    // AssertionValidation (fix wave item 4) now rejects that at save time, so
                    // RecordCase would persist a draft that the very next UpdateCase (even one
                    // editing an unrelated field) refuses to save, with a 400 that names the type
                    // but not which assertion. An assertion that pins nothing isn't worth
                    // recording; skip it. The mock itself is still recorded (draft.Mocks, added
                    // above) so a human reviewing the draft can see and fix the gap.
                    logger?.LogWarning(
                        "Agent test recorder: turn {TurnIndex} of conversation {ConversationId} "
                        + "recorded a function dialog with no function name; skipping the "
                        + "toolCalled assertion that would otherwise have an empty target.",
                        turn.Index, conversationId);
                    continue;
                }

                turn.Assertions.Add(new TestAssertion
                {
                    Type = AssertionTypes.ToolCalled,
                    Target = mock.FunctionName,
                    ArgsMatchJson = mock.ArgsMatchJson
                });
            }
        }

        // Correction (see the task-9 brief's correction section): ToolMockMatcher.Match (Task 5)
        // tries an args-subset match BEFORE falling back to CallIndex. Once a function has been
        // recorded more than once, keeping ArgsMatchJson on its mocks risks a LATER replay call
        // whose real arguments happen to match an EARLIER mock's recorded arguments -- that later
        // call would then resolve to the earlier mock via the args branch, before the ordinal
        // branch is ever consulted, and the case could never reproduce the different result that
        // was actually recorded for it. Ordinal alone is unambiguous once a function repeats. A
        // function recorded exactly once has no later call to collide with, so its ArgsMatchJson
        // stays -- unambiguous, and useful context when a human reviews the draft. This mutates
        // the SAME TestToolMock instances already referenced by draft.Mocks/mocksByTurn AND
        // already copied into the Step 5a assertions above -- it must run AFTER Step 5a, never
        // before (see that step's comment for the bug this ordering fixes).
        foreach (var mock in draft.Mocks)
        {
            if (callCountByFunction.GetValueOrDefault(mock.FunctionName) > 1)
            {
                mock.ArgsMatchJson = null;
            }
        }

        // Step 3: a state value with no MessageId was never written by any turn -- it was seeded
        // before the conversation started.
        foreach (var state in states)
        {
            foreach (var value in state.Values.Where(v => v.MessageId == null))
            {
                draft.InitialStates.Add(new TestState
                {
                    Key = state.Key,
                    Value = value.Data ?? string.Empty,
                    ActiveRounds = value.ActiveRounds
                });
            }
        }

        // Step 4: each turn's OWN state delta (values whose MessageId matches that turn's own
        // MessageId) attaches to the LAST mock created during that turn -- the mock whose
        // (mocked) return is what the real function call actually returned when that state got
        // written. A turn that wrote state but called no mockable function has nowhere to hang
        // the delta -- drop it and say so (there is no other turn it would be correct to
        // attach to).
        foreach (var turn in draft.Turns)
        {
            var turnMessageId = turnMessageIds.GetValueOrDefault(turn.Index);
            if (turnMessageId == null)
            {
                // A turn opened by a user dialog with no MessageId of its own can't be matched
                // against anything -- matching on null here would wrongly pull in the
                // MessageId == null "initial state" values from step 3 above.
                continue;
            }

            var delta = ComputeDelta(states, v => v.MessageId == turnMessageId);
            if (delta.Count == 0)
            {
                continue;
            }

            if (mocksByTurn.TryGetValue(turn.Index, out var turnMocks) && turnMocks.Count > 0)
            {
                turnMocks[^1].StateWrites = delta;
            }
            else
            {
                logger?.LogWarning(
                    "Agent test recorder: turn {TurnIndex} of conversation {ConversationId} wrote "
                    + "{StateCount} state value(s) but called no mockable function during that "
                    + "turn; the delta has nowhere to attach and was dropped.",
                    turn.Index, conversationId, delta.Count);
            }
        }

        // Step 5b: one case-level stateEquals per key that was actually WRITTEN by some turn
        // (i.e. has at least one value with a non-null MessageId), using that key's most recent
        // such write as the expected "final" value the whole recorded conversation left it at. A
        // key that was only ever seeded (every value has MessageId == null, e.g. an auth flag
        // nothing in the conversation ever changes) is already covered by InitialStates above and
        // was never actually incremented by this case, so it is not asserted again here.
        foreach (var state in states)
        {
            var written = state.Values.Where(v => v.MessageId != null).ToList();
            if (written.Count == 0)
            {
                continue;
            }

            draft.Assertions.Add(new TestAssertion
            {
                Type = AssertionTypes.StateEquals,
                Target = state.Key,
                Expected = written[^1].Data
            });
        }

        return draft;
    }

    /// <summary>
    /// What the segmenter is allowed to see: each turn's user message plus the NAMES of the
    /// functions that turn called. Deliberately carries no function arguments and no return
    /// content -- see <see cref="ICaseSegmenter"/> on what does and does not leave this process.
    /// </summary>
    public static List<SegmentableTurn> ToSegmentableTurns(IReadOnlyList<RecordedDialog> dialogs)
    {
        var turns = new List<SegmentableTurn>();
        List<string>? currentTools = null;

        foreach (var dialog in dialogs)
        {
            if (dialog.Role == AgentRole.User)
            {
                currentTools = [];
                turns.Add(new SegmentableTurn
                {
                    Index = turns.Count,
                    UserMessage = dialog.Content ?? string.Empty,
                    ToolNames = currentTools
                });
                continue;
            }

            if (dialog.Role == AgentRole.Function && currentTools != null && !string.IsNullOrWhiteSpace(dialog.FunctionName))
            {
                currentTools.Add(dialog.FunctionName);
            }
        }

        return turns;
    }

    /// <summary>
    /// Pure function: cuts one conversation into several draft cases according to the segmentation.
    /// Each segment runs through <see cref="BuildDraft"/> on its own, so mock return values,
    /// toolCalled assertions and per-turn state writes still come verbatim from the real
    /// conversation -- the segmenter only chose the boundaries and the names.
    ///
    /// Slicing introduces two things <see cref="BuildDraft"/> cannot see and that must be corrected
    /// here:
    ///
    /// 1) **Carried-in state.** A case cut at turn k should start with state as it stood "as of turn
    ///    k-1", but BuildDraft only treats seeded values (MessageId == null) as InitialStates.
    ///    Without this, a later segment starts unable to read the location_id/wo_num the earlier
    ///    turns wrote, and the whole segment runs a path the recording never took.
    ///
    /// 2) **Case-level stateEquals expectations.** BuildDraft's step 5b takes a key's last write
    ///    across the WHOLE conversation. For a segment covering only the first two turns that is a
    ///    final value it never reaches -- the assertion would fail on every single run. Recomputed
    ///    here over the segment's own turns.
    /// </summary>
    public static List<AgentTestCase> BuildDrafts(
        string suiteId,
        string conversationId,
        IReadOnlyList<RecordedDialog> dialogs,
        IReadOnlyList<RecordedState> states,
        IReadOnlyList<CaseSegment> segments,
        ILogger? logger = null)
    {
        // Position in `dialogs` where each user turn opens; a segment's turn indices index into this.
        var turnStarts = new List<int>();
        for (var i = 0; i < dialogs.Count; i++)
        {
            if (dialogs[i].Role == AgentRole.User)
            {
                turnStarts.Add(i);
            }
        }

        var drafts = new List<AgentTestCase>();
        foreach (var segment in segments)
        {
            if (segment.FirstTurn < 0 || segment.LastTurn >= turnStarts.Count || segment.LastTurn < segment.FirstTurn)
            {
                // The segmenter validates its own output, so reaching here means a caller built
                // segments some other way. Skip rather than throw: the segments that ARE valid are
                // still worth producing.
                logger?.LogWarning(
                    "Agent test recorder: dropping segment '{Name}' ({First}..{Last}) -- conversation "
                    + "{ConversationId} only has {TurnCount} turn(s).",
                    segment.Name, segment.FirstTurn, segment.LastTurn, conversationId, turnStarts.Count);
                continue;
            }

            var start = turnStarts[segment.FirstTurn];
            var end = segment.LastTurn + 1 < turnStarts.Count
                ? turnStarts[segment.LastTurn + 1] - 1
                : dialogs.Count - 1;

            var slice = new List<RecordedDialog>();
            for (var i = start; i <= end; i++)
            {
                slice.Add(dialogs[i]);
            }

            var draft = BuildDraft(suiteId, conversationId, slice, states, logger);
            draft.Name = segment.Name;

            // State is keyed by the USER turn's own MessageId (see step 4 above), so both fixes
            // below are computed from turn message ids, not from every dialog's id.
            if (segment.FirstTurn > 0)
            {
                var priorTurnIds = turnStarts
                    .Take(segment.FirstTurn)
                    .Select(pos => dialogs[pos].MessageId)
                    .Where(id => id != null)
                    .ToHashSet(StringComparer.Ordinal);

                // Superset of what BuildDraft computed (seeded values), with later writes winning,
                // so replacing wholesale is correct rather than merging.
                draft.InitialStates = ComputeDelta(
                    states, v => v.MessageId == null || (v.MessageId != null && priorTurnIds.Contains(v.MessageId)));
            }

            var sliceTurnIds = turnStarts
                .Skip(segment.FirstTurn)
                .Take(segment.LastTurn - segment.FirstTurn + 1)
                .Select(pos => dialogs[pos].MessageId)
                .Where(id => id != null)
                .ToHashSet(StringComparer.Ordinal);

            draft.Assertions = states
                .Select(state => new
                {
                    state.Key,
                    Written = state.Values
                        .Where(v => v.MessageId != null && sliceTurnIds.Contains(v.MessageId))
                        .ToList()
                })
                .Where(x => x.Written.Count > 0)
                .Select(x => new TestAssertion
                {
                    Type = AssertionTypes.StateEquals,
                    Target = x.Key,
                    Expected = x.Written[^1].Data
                })
                .ToList();

            drafts.Add(draft);
        }

        return drafts;
    }

    private static List<TestState> ComputeDelta(IReadOnlyList<RecordedState> states, Func<RecordedStateValue, bool> predicate)
    {
        var delta = new List<TestState>();

        foreach (var state in states)
        {
            var match = state.Values.LastOrDefault(predicate);
            if (match != null)
            {
                delta.Add(new TestState
                {
                    Key = state.Key,
                    Value = match.Data ?? string.Empty,
                    ActiveRounds = match.ActiveRounds
                });
            }
        }

        return delta;
    }
}

/// <summary>
/// One dialog entry as recording sees it -- the fields recording cares about, picked out of the real
/// <c>DialogElement</c>/<c>DialogMetaData</c>.
/// </summary>
public class RecordedDialog
{
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FunctionName { get; set; }
    public string? FunctionArgs { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// One state key and its full history of values as recording sees it -- the fields recording cares
/// about, picked out of the real <c>StateKeyValue</c>.
/// </summary>
public class RecordedState
{
    public string Key { get; set; } = string.Empty;
    public List<RecordedStateValue> Values { get; set; } = [];
}

/// <summary>
/// See <see cref="RecordedState"/> -- one historical value, corresponding to a real
/// <c>StateValue</c>/<c>StateValueMongoElement</c>.
/// </summary>
public class RecordedStateValue
{
    public string? MessageId { get; set; }
    public string? Data { get; set; }
    public int ActiveRounds { get; set; }
}
