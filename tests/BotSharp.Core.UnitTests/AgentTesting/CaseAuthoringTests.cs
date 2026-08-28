using System;
using System.Collections.Generic;
using System.Linq;
using BotSharp.Plugin.AgentTesting.Models;
using BotSharp.Plugin.AgentTesting.Services;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Authoring a case by conversation hands a model write access to a document a human is editing. The
/// four things stopping that from going wrong are all in <see cref="LlmCaseAuthor.Assemble"/> and
/// <see cref="LlmCaseAuthor.Parse"/>, and this is where they are pinned:
///
/// 1. a field the model does not declare cannot be deleted by being left out of its answer;
/// 2. a mock or tool assertion naming a function the agent cannot call is removed, not stored;
/// 3. a draft that would fail on save comes back labelled, not presented as progress;
/// 4. the diff shown to the user is computed from the two drafts, never read off the model's own
///    account of what it did.
///
/// No vendor is involved: both methods are pure, which is why they are public.
/// </summary>
public class CaseAuthoringTests
{
    private static AgentTestSuite Suite(string? judgeProvider = null, string? judgeModel = null) => new()
    {
        Id = "suite-1",
        AgentId = "agent-1",
        Name = "suite",
        Enabled = true,
        JudgeProvider = judgeProvider,
        JudgeModel = judgeModel
    };

    private static List<MockTargetInfo> Targets(params string[] names)
        => names.Select(n => new MockTargetInfo(n, null, null)).ToList();

    /// <summary>A saved case with one turn and one mock -- the thing an authoring turn can damage.</summary>
    private static AgentTestCaseUpsertRequest Baseline() => new()
    {
        SuiteId = "suite-1",
        Name = "where is my technician",
        Turns = [new TestTurn { Index = 0, UserMessage = "where is my tech" }],
        Mocks = [new TestToolMock { FunctionName = "get_eta", ResultContent = """{"eta":"2pm"}""" }]
    };

    private static AuthorAttempt Attempt(
        string[] declared,
        AgentTestCaseUpsertRequest? draft,
        string reply = "Added a turn.",
        string[]? rejected = null)
        => new(reply, declared.ToList(), (rejected ?? []).ToList(), draft, "{}");

    private static AgentTestAuthorResponse Run(
        AuthorAttempt attempt,
        AgentTestCaseUpsertRequest? baseline = null,
        List<MockTargetInfo>? targets = null,
        List<AgentTestCase>? existing = null,
        AgentTestSuite? suite = null)
        => LlmCaseAuthor.Assemble(
            baseline ?? Baseline(),
            attempt,
            targets ?? Targets("get_eta", "reschedule"),
            existing ?? [],
            suite ?? Suite());

    // ---- 1. A field the model does not declare survives -------------------------------------

    [Fact]
    public void An_undeclared_field_is_kept_even_when_the_model_returns_it_empty()
    {
        // The whole reason for the declared-fields whitelist. A model that returns the document with
        // `mocks` missing is the normal failure of asking for a full document back, and taking its
        // answer wholesale would delete a mock nobody asked it to touch -- silently, because an
        // absent mock only shows up as the case failing on its next run.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Name = "where is my technician",
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "where is my tech" },
                new TestTurn { Index = 1, UserMessage = "and the address?" }
            ],
            Mocks = []
        };

        var response = Run(Attempt(["turns"], modelDraft));

        Assert.Single(response.Draft.Mocks);
        Assert.Equal("get_eta", response.Draft.Mocks[0].FunctionName);
        Assert.Equal(2, response.Draft.Turns.Count);
    }

    [Fact]
    public void A_declared_field_is_applied()
    {
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "where is my tech" },
                new TestTurn { Index = 1, UserMessage = "and the address?" }
            ]
        };

        var response = Run(Attempt(["turns"], modelDraft));

        Assert.True(response.DraftChanged);
        Assert.Contains(response.Changes, c => c.Field == "turns");
        Assert.Equal("1 -> 2 item(s)", response.Changes.Single(c => c.Field == "turns").Detail);
    }

    [Fact]
    public void A_field_the_model_may_not_change_is_ignored_and_reported()
    {
        // AuthorFields.Normalize returns null for anything unwritable, and Parse routes those into
        // RejectedFields. Reported rather than dropped in silence: "I renamed the suite for you"
        // needs to be visibly untrue.
        var response = Run(Attempt([], null, reply: "Renamed the suite.", rejected: ["suiteId"]));

        Assert.False(response.DraftChanged);
        Assert.Contains(response.Warnings, w => w.Contains("suiteId"));
    }

    [Fact]
    public void The_unmocked_tool_policy_cannot_be_moved_off_block()
    {
        // Not a writable field, so a model proposing Passthrough gets Block anyway. Worth its own
        // test because Passthrough is the one setting under which toolNotCalled passes vacuously
        // against a tool that really executed.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Turns = [new TestTurn { Index = 0, UserMessage = "where is my tech" }],
            UnmockedToolPolicy = "Passthrough"
        };

        var response = Run(Attempt(["turns"], modelDraft));

        Assert.Equal(UnmockedToolPolicies.Block, response.Draft.UnmockedToolPolicy);
        Assert.Empty(response.ValidationErrors);
    }

    [Fact]
    public void Declaring_a_field_without_returning_a_draft_changes_nothing()
    {
        var response = Run(Attempt(["turns"], null));

        Assert.False(response.DraftChanged);
        Assert.Single(response.Draft.Turns);
        Assert.Contains(response.Warnings, w => w.Contains("returned no draft"));
    }

    // ---- 2. Never a function the agent cannot call ------------------------------------------

    [Fact]
    public void A_mock_for_an_unknown_function_is_dropped()
    {
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Mocks =
            [
                new TestToolMock { FunctionName = "get_eta", ResultContent = "{}" },
                new TestToolMock { FunctionName = "send_sms_to_resident", ResultContent = "{}" }
            ]
        };

        var response = Run(Attempt(["mocks"], modelDraft));

        Assert.Single(response.Draft.Mocks);
        Assert.Equal("get_eta", response.Draft.Mocks[0].FunctionName);
        Assert.Contains(response.Warnings, w => w.Contains("send_sms_to_resident"));
    }

    [Fact]
    public void A_tool_assertion_on_an_unknown_function_is_dropped()
    {
        // Kept, it would never match at run time, so the case would fail forever for a reason that
        // reads as an agent regression.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Assertions =
            [
                new TestAssertion { Type = AssertionTypes.ToolCalled, Target = "get_eta" },
                new TestAssertion { Type = AssertionTypes.ToolCalled, Target = "get_estimated_arrival" }
            ]
        };

        var response = Run(Attempt(["assertions"], modelDraft));

        Assert.Single(response.Draft.Assertions);
        Assert.Equal("get_eta", response.Draft.Assertions[0].Target);
        Assert.Contains(response.Warnings, w => w.Contains("get_estimated_arrival"));
    }

    [Fact]
    public void A_non_tool_assertion_is_never_dropped_for_its_target()
    {
        // stateEquals also carries a Target, and it is a state key, not a function name.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Assertions = [new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_num", Expected = "B1" }]
        };

        var response = Run(Attempt(["assertions"], modelDraft));

        Assert.Single(response.Draft.Assertions);
    }

    [Fact]
    public void An_unfamiliar_state_key_is_kept_but_flagged()
    {
        // The asymmetry with function names is deliberate: nothing in this system enumerates the keys
        // an agent writes, so the "known" list is only what other cases happen to use. Dropping on
        // absence from an admittedly incomplete list would delete correct work.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Assertions = [new TestAssertion { Type = AssertionTypes.StateEquals, Target = "invented_key", Expected = "x" }]
        };

        var response = Run(Attempt(["assertions"], modelDraft));

        Assert.Single(response.Draft.Assertions);
        Assert.Contains(response.Warnings, w => w.Contains("invented_key"));
    }

    [Fact]
    public void A_state_key_another_case_already_uses_is_not_flagged()
    {
        var existing = new List<AgentTestCase>
        {
            new()
            {
                Id = "case-9",
                SuiteId = "suite-1",
                Name = "other",
                InitialStates = [new TestState { Key = "wo_num", Value = "B1" }]
            }
        };

        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Assertions = [new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_num", Expected = "B1" }]
        };

        var response = Run(Attempt(["assertions"], modelDraft), existing: existing);

        Assert.DoesNotContain(response.Warnings, w => w.Contains("wo_num"));
    }

    [Fact]
    public void An_llm_judge_assertion_on_a_suite_with_no_judge_model_is_flagged()
    {
        // It would not fail validation -- it would save cleanly and then fail every run, which is the
        // sort of thing worth saying at authoring time.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Assertions = [new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "polite and specific" }]
        };

        var withoutJudge = Run(Attempt(["assertions"], modelDraft));
        Assert.Contains(withoutJudge.Warnings, w => w.Contains("judge model"));

        var withJudge = Run(Attempt(["assertions"], modelDraft), suite: Suite("openai", "gpt-4o"));
        Assert.DoesNotContain(withJudge.Warnings, w => w.Contains("judge model"));
    }

    // ---- 3. An invalid draft is labelled, not presented as progress -------------------------

    [Fact]
    public void A_routing_case_the_model_gave_two_turns_comes_back_with_the_validation_error()
    {
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            CaseType = CaseTypes.Routing,
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "my fridge leaks" },
                new TestTurn { Index = 1, UserMessage = "when will someone come" }
            ],
            Assertions = [new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Work Order Agent" }]
        };

        var response = Run(Attempt(["caseType", "turns", "assertions"], modelDraft));

        Assert.NotEmpty(response.ValidationErrors);
        Assert.Contains("exactly one turn", response.ValidationErrors[0]);
    }

    [Fact]
    public void A_routing_case_that_asserts_no_routing_outcome_is_rejected()
    {
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            CaseType = CaseTypes.Routing,
            Turns = [new TestTurn { Index = 0, UserMessage = "my fridge leaks" }],
            Assertions = [new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "sorry" }]
        };

        var response = Run(Attempt(["caseType", "turns", "assertions"], modelDraft));

        Assert.NotEmpty(response.ValidationErrors);
    }

    [Fact]
    public void Turns_are_renumbered_from_zero()
    {
        // A model inserting a turn tends to renumber badly or not at all, and the runner reads turns
        // in order -- so a stale Index would reorder the case rather than fail it.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Turns =
            [
                new TestTurn { Index = 5, UserMessage = "first" },
                new TestTurn { Index = 5, UserMessage = "second" }
            ]
        };

        var response = Run(Attempt(["turns"], modelDraft));

        Assert.Equal([0, 1], response.Draft.Turns.Select(t => t.Index));
    }

    // ---- 4. The diff is computed, not claimed -----------------------------------------------

    [Fact]
    public void A_declared_field_that_did_not_actually_change_is_not_reported_as_changed()
    {
        // The model claims two fields; only one differs. What the user sees has to be the second
        // number, or the change list stops being evidence of anything.
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            Name = "where is my technician",
            Turns =
            [
                new TestTurn { Index = 0, UserMessage = "where is my tech" },
                new TestTurn { Index = 1, UserMessage = "and the address?" }
            ]
        };

        var response = Run(Attempt(["name", "turns"], modelDraft));

        Assert.Contains(response.Changes, c => c.Field == "turns");
        Assert.DoesNotContain(response.Changes, c => c.Field == "name");
    }

    [Fact]
    public void An_answer_that_declares_nothing_is_a_question_not_a_no_op_edit()
    {
        // The clarifying-question path. It has to leave the draft untouched and still deliver the
        // reply, or the model cannot ask anything without also having to invent an edit.
        var response = Run(Attempt([], null, reply: "Which work order should the case use?"));

        Assert.False(response.DraftChanged);
        Assert.Empty(response.Changes);
        Assert.Equal("Which work order should the case use?", response.Reply);
        Assert.Single(response.Draft.Turns);
        Assert.Single(response.Draft.Mocks);
    }

    [Fact]
    public void The_suite_id_always_comes_from_the_request()
    {
        var modelDraft = new AgentTestCaseUpsertRequest
        {
            SuiteId = "some-other-suite",
            Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
        };

        var response = Run(Attempt(["turns"], modelDraft));

        Assert.Equal("suite-1", response.Draft.SuiteId);
    }

    // ---- Parse: reading the envelope --------------------------------------------------------

    [Fact]
    public void Parse_normalises_declared_field_names_and_separates_the_unwritable_ones()
    {
        var attempt = LlmCaseAuthor.Parse(
            """{"reply":"ok","changedFields":["Turns","lastReviewedDate","turns"],"draft":{"name":"x"}}""");

        Assert.Equal("ok", attempt.Reply);
        Assert.Equal(["turns"], attempt.DeclaredFields);
        Assert.Equal(["lastReviewedDate"], attempt.RejectedFields);
        Assert.Equal("x", attempt.Draft?.Name);
    }

    [Fact]
    public void Parse_tolerates_a_code_fence_and_surrounding_prose()
    {
        // The commonest and most harmless way for a model to disobey "JSON only" -- the segmenter
        // takes the same view.
        var attempt = LlmCaseAuthor.Parse(
            "Here you go:\n```json\n{\"reply\":\"ok\",\"changedFields\":[],\"draft\":null}\n```");

        Assert.Equal("ok", attempt.Reply);
        Assert.Empty(attempt.DeclaredFields);
    }

    [Fact]
    public void Parse_treats_a_missing_changed_fields_list_as_no_change()
    {
        var attempt = LlmCaseAuthor.Parse("""{"reply":"I need to know the work order number first."}""");

        Assert.Empty(attempt.DeclaredFields);
        Assert.Null(attempt.Draft);
    }

    [Theory]
    [InlineData("I could not do that.")]
    [InlineData("")]
    [InlineData("{ not json at all ")]
    public void Parse_rejects_an_answer_it_cannot_read(string raw)
    {
        // No draft was produced, which is a different outcome from an invalid draft: the caller
        // should retry, not review.
        Assert.Throws<CaseAuthorUnavailableException>(() => LlmCaseAuthor.Parse(raw));
    }

    // ---- Parse: coercing a JSON-as-text field written as a nested object -------------------
    //
    // The exact shape reported in production: a model writes argsMatchJson as a real object because
    // that is the natural way to express "match these arguments", even though the field is a string
    // holding escaped JSON. Both forms are syntactically valid JSON, so nothing before deserialization
    // can reject this as malformed -- it has to be normalised before the strongly-typed model ever
    // sees it, or every case-level and turn-level argsMatchJson blows up the whole reply.

    [Fact]
    public void An_object_valued_argsMatchJson_on_a_case_level_assertion_is_coerced_to_its_json_text()
    {
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["assertions"],"draft":{"assertions":[
                {"type":"toolCalled","target":"get_eta","argsMatchJson":{"work_order_id":"12345"}}
            ]}}
            """);

        var assertion = Assert.Single(attempt.Draft!.Assertions);
        Assert.Equal("""{"work_order_id":"12345"}""", assertion.ArgsMatchJson);
    }

    [Fact]
    public void An_object_valued_argsMatchJson_on_a_turn_level_assertion_is_also_coerced()
    {
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["turns"],"draft":{"turns":[
                {"index":0,"userMessage":"where is my tech","assertions":[
                    {"type":"toolCalled","target":"get_eta","argsMatchJson":{"wo_num":"B1"}}
                ]}
            ]}}
            """);

        var assertion = Assert.Single(attempt.Draft!.Turns[0].Assertions);
        Assert.Equal("""{"wo_num":"B1"}""", assertion.ArgsMatchJson);
    }

    [Fact]
    public void An_object_valued_mock_result_content_and_args_match_are_both_coerced()
    {
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["mocks"],"draft":{"mocks":[
                {"functionName":"get_eta","argsMatchJson":{"wo_num":"B1"},"resultContent":{"eta":"2pm"}}
            ]}}
            """);

        var mock = Assert.Single(attempt.Draft!.Mocks);
        Assert.Equal("""{"wo_num":"B1"}""", mock.ArgsMatchJson);
        Assert.Equal("""{"eta":"2pm"}""", mock.ResultContent);
    }

    [Fact]
    public void An_array_valued_mock_state_write_value_is_coerced()
    {
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["mocks"],"draft":{"mocks":[
                {"functionName":"get_eta","stateWrites":[{"key":"items","value":[1,2,3]}]}
            ]}}
            """);

        Assert.Equal("[1,2,3]", attempt.Draft!.Mocks[0].StateWrites![0].Value);
    }

    [Fact]
    public void An_object_valued_initial_state_value_is_coerced()
    {
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["initialStates"],"draft":{"initialStates":[
                {"key":"customer","value":{"name":"Jane"}}
            ]}}
            """);

        Assert.Equal("""{"name":"Jane"}""", attempt.Draft!.InitialStates[0].Value);
    }

    [Fact]
    public void A_string_valued_argsMatchJson_passes_through_unchanged()
    {
        // The correct, already-escaped form must not be touched -- only the wrong shape is coerced.
        var attempt = LlmCaseAuthor.Parse(
            """
            {"reply":"ok","changedFields":["assertions"],"draft":{"assertions":[
                {"type":"toolCalled","target":"get_eta","argsMatchJson":"{\"wo_num\":\"B1\"}"}
            ]}}
            """);

        Assert.Equal("""{"wo_num":"B1"}""", attempt.Draft!.Assertions[0].ArgsMatchJson);
    }

    [Fact]
    public void A_genuine_json_syntax_error_still_fails_instead_of_being_silently_swallowed()
    {
        // The coercion pass must not mask a real syntax error by quietly returning the input
        // unchanged and hoping for the best -- it has to fall through to the same rejection an
        // unrecoverable reply always gets.
        Assert.Throws<CaseAuthorUnavailableException>(() => LlmCaseAuthor.Parse(
            """{"reply":"ok","changedFields":["assertions"],"draft":{"assertions":[{,}]}}"""));
    }
}
