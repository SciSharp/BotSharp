using System.Collections.Generic;
using System.Linq;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Recording is what decides whether QA and PM can actually use this feature: hand-writing a work
/// order agent's mock JSON is not realistic.
///
/// Two deliberate limitations are pinned here; changing either means changing the spec first:
/// 1) state writes can only be extracted as a whole-turn delta -- StateValueMongoElement carries only
///    MessageId (which locates a turn) and a Source of external/application/user, never a function
///    name, so splitting across individual mocks automatically is not possible;
/// 2) no outputContains-style assertions are generated -- using the model's exact wording as a
///    baseline is extremely brittle, any rephrasing goes red, and recording one case would mean
///    hand-editing ten assertions.
/// </summary>
public class AgentTestRecorderTests
{
    private static readonly List<RecordedDialog> Dialogs =
    [
        new() { Role = "user", Content = "my sink is leaking", MessageId = "m1" },
        new() { Role = "assistant", FunctionName = "get_work_order", FunctionArgs = """{"woNum":"B1"}""", MessageId = "m1" },
        new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Open"}""", MessageId = "m1" },
        new() { Role = "assistant", Content = "I found work order B1.", MessageId = "m1" },
        new() { Role = "user", Content = "please schedule it", MessageId = "m2" },
        new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Scheduled"}""", MessageId = "m2" },
        new() { Role = "assistant", Content = "Scheduled for tomorrow.", MessageId = "m2" }
    ];

    private static readonly List<RecordedState> States =
    [
        new() { Key = "user_authenticated", Values = [new() { MessageId = null, Data = "true", ActiveRounds = -1 }] },
        new() { Key = "wo_id", Values = [new() { MessageId = "m1", Data = "123", ActiveRounds = -1 }] }
    ];

    private static AgentTestCase Draft() => AgentTestRecorder.BuildDraft("suite-1", "conv-9", Dialogs, States);

    [Fact]
    public void Takes_the_user_messages_as_turns_in_order()
    {
        var draft = Draft();

        Assert.Equal(2, draft.Turns.Count);
        Assert.Equal("my sink is leaking", draft.Turns[0].UserMessage);
        Assert.Equal("please schedule it", draft.Turns[1].UserMessage);
        Assert.Equal([0, 1], draft.Turns.Select(t => t.Index));
    }

    [Fact]
    public void Turns_the_real_function_results_into_mocks()
    {
        var draft = Draft();

        Assert.Equal(2, draft.Mocks.Count);
        Assert.All(draft.Mocks, m => Assert.Equal("get_work_order", m.FunctionName));
        Assert.Equal("""{"status":"Open"}""", draft.Mocks[0].ResultContent);
        Assert.Equal("""{"status":"Scheduled"}""", draft.Mocks[1].ResultContent);
    }

    [Fact]
    public void Numbers_repeated_calls_of_the_same_function_so_they_can_be_told_apart()
    {
        // The same function called twice with different returns: without an ordinal both calls would
        // resolve to the same fake return.
        var draft = Draft();

        Assert.Equal(0, draft.Mocks[0].CallIndex);
        Assert.Equal(1, draft.Mocks[1].CallIndex);
    }

    [Fact]
    public void Omits_recorded_arguments_for_a_repeated_function_but_keeps_them_for_a_function_recorded_once()
    {
        // Correction to the original brief: ToolMockMatcher.Match (Task 5) tries an args-subset
        // match BEFORE falling back to CallIndex. get_work_order is recorded twice in the shared
        // fixture above (Dialogs) -- if Mocks[0] kept ArgsMatchJson = {"woNum":"B1"}, a replay
        // call in turn 2 that happens to carry the SAME arguments (very plausible: the agent is
        // very likely to still be talking about the same work order) would match Mocks[0] via
        // the args branch before the ordinal branch is ever reached, and the case could never
        // reproduce the different result ("Scheduled") that was actually recorded for that
        // second call. Once a function repeats, only CallIndex is unambiguous.
        //
        // This uses its own local fixture (rather than extending the shared Dialogs/States
        // above) specifically so it does NOT shift Mocks[]/Turns[] indices or counts out from
        // under every other test in this file that pins them (e.g. Assert.Equal(2,
        // draft.Mocks.Count) in Turns_the_real_function_results_into_mocks).
        List<RecordedDialog> dialogs =
        [
            new() { Role = "user", Content = "my sink is leaking, I'm customer C9", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "check_customer", FunctionArgs = """{"customerId":"C9"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "check_customer", Content = """{"tier":"gold"}""", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "get_work_order", FunctionArgs = """{"woNum":"B1"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Open"}""", MessageId = "m1" },
            new() { Role = "assistant", Content = "I found work order B1.", MessageId = "m1" },
            new() { Role = "user", Content = "please schedule it", MessageId = "m2" },
            new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Scheduled"}""", MessageId = "m2" },
            new() { Role = "assistant", Content = "Scheduled for tomorrow.", MessageId = "m2" }
        ];

        var draft = AgentTestRecorder.BuildDraft("suite-1", "conv-9", dialogs, []);

        // check_customer was recorded exactly once: unambiguous, so its recorded arguments are
        // kept (useful context when a human reviews the draft in the editor).
        var checkCustomer = draft.Mocks.Single(m => m.FunctionName == "check_customer");
        Assert.Equal("""{"customerId":"C9"}""", checkCustomer.ArgsMatchJson);

        // get_work_order was recorded twice: BOTH of its mocks must drop ArgsMatchJson so that
        // ToolMockMatcher.Match can only ever tell them apart by CallIndex.
        var getWorkOrders = draft.Mocks.Where(m => m.FunctionName == "get_work_order").ToList();
        Assert.Equal(2, getWorkOrders.Count);
        Assert.All(getWorkOrders, m => Assert.Null(m.ArgsMatchJson));
        Assert.Equal([0, 1], getWorkOrders.Select(m => m.CallIndex));
    }

    [Fact]
    public void Only_states_without_a_message_id_become_initial_states()
    {
        var draft = Draft();

        var initial = Assert.Single(draft.InitialStates);
        Assert.Equal("user_authenticated", initial.Key);
        Assert.Equal("true", initial.Value);
    }

    [Fact]
    public void The_turns_state_delta_is_attached_to_the_last_mock_of_that_turn()
    {
        var draft = Draft();

        var write = Assert.Single(draft.Mocks[0].StateWrites!);
        Assert.Equal("wo_id", write.Key);
        Assert.Equal("123", write.Value);
        Assert.Null(draft.Mocks[1].StateWrites);
    }

    [Fact]
    public void Keeps_the_recorded_arguments_on_the_toolCalled_assertion_even_when_the_mock_omits_them()
    {
        // Fix round 1: the args/ordinal correction (see
        // Omits_recorded_arguments_for_a_repeated_function_but_keeps_them_for_a_function_recorded_once)
        // is needed because ToolMockMatcher.Match dispatches against the WHOLE case's mock list
        // (MockFunctionExecutor.ExecuteAsync calls Match(_run.Mocks, ...), not scoped to one
        // turn) -- that really is ambiguous across turns once a function repeats. But
        // AssertionEvaluator's toolCalled case is evaluated per turn, against ONLY that turn's
        // observed calls (AgentTestCaseRunner.cs: active.ObservedCalls.Where(c => c.TurnIndex ==
        // turn.Index)) -- there is no cross-turn collision for an assertion to guard against, so
        // it must keep the argument actually recorded for THIS turn's call even though the mock
        // behind it had to drop it to stay unambiguous for dispatch.
        var draft = Draft();

        // The mock lost its args (get_work_order is recorded twice across the whole case).
        Assert.Null(draft.Mocks[0].ArgsMatchJson);

        // The toolCalled assertion generated for that same turn 1 call did not.
        var assertion = Assert.Single(draft.Turns[0].Assertions);
        Assert.Equal(AssertionTypes.ToolCalled, assertion.Type);
        Assert.Equal("get_work_order", assertion.Target);
        Assert.Equal("""{"woNum":"B1"}""", assertion.ArgsMatchJson);
    }

    [Fact]
    public void Keeps_each_calls_own_arguments_when_the_same_function_is_recorded_twice_in_one_turn()
    {
        // Fix round 1, small item 2: the only existing coverage for "recorded more than once"
        // spans two DIFFERENT turns. This pins the same mechanism within a SINGLE turn: two calls
        // to the same function, each with its own preceding assistant FunctionArgs entry.
        // pendingArgsByFunction's last-write-wins is exactly "nearest preceding" per call, and the
        // correction (and the fix above, which generates assertions before it runs) is turn-
        // agnostic, so both calls should keep their own distinct recorded arguments on their
        // toolCalled assertions while both mocks omit ArgsMatchJson and rely on CallIndex alone.
        List<RecordedDialog> dialogs =
        [
            new() { Role = "user", Content = "check work orders B1 and B2", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "get_work_order", FunctionArgs = """{"woNum":"B1"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Open"}""", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "get_work_order", FunctionArgs = """{"woNum":"B2"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Closed"}""", MessageId = "m1" },
            new() { Role = "assistant", Content = "B1 is open, B2 is closed.", MessageId = "m1" }
        ];

        var draft = AgentTestRecorder.BuildDraft("suite-1", "conv-9", dialogs, []);

        Assert.Equal(2, draft.Mocks.Count);
        Assert.All(draft.Mocks, m => Assert.Null(m.ArgsMatchJson));
        Assert.Equal([0, 1], draft.Mocks.Select(m => m.CallIndex));
        Assert.Equal("""{"status":"Open"}""", draft.Mocks[0].ResultContent);
        Assert.Equal("""{"status":"Closed"}""", draft.Mocks[1].ResultContent);

        var turn0Assertions = Assert.Single(draft.Turns).Assertions;
        Assert.Equal(2, turn0Assertions.Count);
        Assert.Equal("""{"woNum":"B1"}""", turn0Assertions[0].ArgsMatchJson);
        Assert.Equal("""{"woNum":"B2"}""", turn0Assertions[1].ArgsMatchJson);
    }

    [Fact]
    public void Treats_differently_cased_recordings_of_the_same_function_as_one_function()
    {
        // Fix wave item 8d: pendingArgsByFunction/callCountByFunction used to compare function
        // names Ordinal while ToolMockMatcher.Match (and ActiveTestRun's own call-ordinal
        // tracker) compare OrdinalIgnoreCase -- two differently-cased recordings of the SAME real
        // function were two functions to the recorder (each counted once, so neither's
        // ArgsMatchJson got stripped and both got CallIndex 0) but one function to the matcher,
        // which could then resolve either replay call to either mock. This dialog fixture is
        // otherwise identical to the two-calls-same-function case already covered above, just
        // with the second call's FunctionName differently cased.
        List<RecordedDialog> dialogs =
        [
            new() { Role = "user", Content = "check work order B1 then again", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "Get_Work_Order", FunctionArgs = """{"woNum":"B1"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "Get_Work_Order", Content = """{"status":"Open"}""", MessageId = "m1" },
            new() { Role = "assistant", FunctionName = "get_work_order", FunctionArgs = """{"woNum":"B1"}""", MessageId = "m1" },
            new() { Role = "function", FunctionName = "get_work_order", Content = """{"status":"Scheduled"}""", MessageId = "m1" },
            new() { Role = "assistant", Content = "done", MessageId = "m1" }
        ];

        var draft = AgentTestRecorder.BuildDraft("suite-1", "conv-9", dialogs, []);

        Assert.Equal(2, draft.Mocks.Count);
        // Recognized as the SAME function called twice -> ordinal-only, ArgsMatchJson dropped on
        // both, exactly like the case-consistent repeated-function fixture above. Before the fix,
        // each cased variant was counted once and both mocks would have kept their ArgsMatchJson.
        Assert.All(draft.Mocks, m => Assert.Null(m.ArgsMatchJson));
        Assert.Equal([0, 1], draft.Mocks.Select(m => m.CallIndex));
        Assert.Equal("""{"status":"Open"}""", draft.Mocks[0].ResultContent);
        Assert.Equal("""{"status":"Scheduled"}""", draft.Mocks[1].ResultContent);
    }

    [Fact]
    public void A_function_dialog_with_no_function_name_produces_a_mock_but_no_pinning_assertion()
    {
        // Coordinator re-review item 3: a Role=Function dialog with a null/blank FunctionName
        // used to still generate a toolCalled assertion with a blank Target -- which
        // AssertionValidation (fix wave item 4) now rejects at save time, so RecordCase could
        // persist a draft that the very next UpdateCase (even one editing an unrelated field)
        // refused to save, from a 400 that names the type but not which assertion. An assertion
        // that pins nothing isn't worth recording; the fix skips generating it while still
        // recording the mock itself, so a human reviewing the draft can see and fix the gap.
        List<RecordedDialog> dialogs =
        [
            new() { Role = "user", Content = "do something", MessageId = "m1" },
            new() { Role = "function", FunctionName = null, Content = "some result", MessageId = "m1" },
            new() { Role = "assistant", Content = "done", MessageId = "m1" }
        ];

        var draft = AgentTestRecorder.BuildDraft("suite-1", "conv-9", dialogs, []);

        var mock = Assert.Single(draft.Mocks);
        Assert.Equal(string.Empty, mock.FunctionName);
        Assert.Empty(Assert.Single(draft.Turns).Assertions);
    }

    [Fact]
    public void Suggests_only_stable_assertion_types()
    {
        var draft = Draft();

        var types = draft.Turns.SelectMany(t => t.Assertions).Concat(draft.Assertions)
            .Select(a => a.Type).Distinct().ToList();

        Assert.Contains(AssertionTypes.ToolCalled, types);
        Assert.Contains(AssertionTypes.StateEquals, types);
        Assert.DoesNotContain(AssertionTypes.OutputContains, types);
        Assert.DoesNotContain(AssertionTypes.OutputRegex, types);
        Assert.DoesNotContain(AssertionTypes.LlmJudge, types);
    }

    [Fact]
    public void The_draft_is_disabled_and_remembers_where_it_came_from()
    {
        var draft = Draft();

        Assert.False(draft.Enabled);                       // enabled only after a human reviews it
        Assert.Equal("conv-9", draft.SourceConversationId);
        Assert.Equal(UnmockedToolPolicies.Block, draft.UnmockedToolPolicy);
    }

    [Fact]
    public void A_conversation_with_no_user_message_produces_an_empty_draft_rather_than_throwing()
    {
        var draft = AgentTestRecorder.BuildDraft("suite-1", "conv-x",
            [new RecordedDialog { Role = "assistant", Content = "hello" }], []);

        Assert.Empty(draft.Turns);
    }
}
