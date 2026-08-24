using System.Collections.Generic;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Assertion evaluation IS the pass/fail verdict, so it has to be pure and exhaustively testable.
/// Pinned type by type, including the edges that are easy to get wrong: an invalid regex must not
/// blow the case up into an Error (users do write bad regexes), toolCalled's argument matching is a
/// subset rather than an exact match (otherwise the author would have to list every argument the
/// model passes), and stateEquals distinguishes "the value differs" from "the key is not set at
/// all".
/// </summary>
public class AssertionEvaluatorTests
{
    private static AssertionContext Context(
        string? output = null,
        IReadOnlyList<ObservedToolCall>? calls = null,
        IReadOnlyDictionary<string, string?>? states = null,
        string? routedTo = null) => new()
        {
            Output = output,
            ToolCalls = calls ?? [],
            States = states ?? new Dictionary<string, string?>(),
            // routedToAgent reads the chain's last entry, so a one-hop chain IS "routed to this
            // agent". Given a name only, the id is left blank -- which is also what an agent whose
            // record cannot be loaded looks like.
            AgentChain = routedTo == null ? [] : [new AgentChainHop { Id = string.Empty, Name = routedTo }]
        };

    [Fact]
    public void Output_contains_passes_on_a_substring_and_reports_the_actual_output()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "work order" },
            Context(output: "I created the work order for you."));

        Assert.True(result.Passed);
        Assert.Equal("I created the work order for you.", result.Actual);
    }

    [Fact]
    public void Output_contains_is_case_insensitive()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputContains, Expected = "WORK ORDER" },
            Context(output: "I created the work order."));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Output_not_contains_fails_when_the_phrase_appears()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputNotContains, Expected = "sorry" },
            Context(output: "Sorry, I cannot help."));

        Assert.False(result.Passed);
    }

    [Fact]
    public void Output_regex_reports_a_bad_pattern_as_a_failed_assertion_not_an_exception()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputRegex, Expected = "([unclosed" },
            Context(output: "anything"));

        Assert.False(result.Passed);
        Assert.Contains("invalid regular expression", result.Message!);
    }

    [Fact]
    public void Tool_called_matches_arguments_as_a_subset()
    {
        var calls = new List<ObservedToolCall>
        {
            new() { FunctionName = "get_work_order", ArgsJson = """{"woNum":"B1","notes":true}""", Outcome = "Mocked" }
        };

        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.ToolCalled,
                Target = "get_work_order",
                ArgsMatchJson = """{"woNum":"B1"}"""
            },
            Context(calls: calls));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Tool_called_fails_when_arguments_do_not_match()
    {
        var calls = new List<ObservedToolCall>
        {
            new() { FunctionName = "get_work_order", ArgsJson = """{"woNum":"OTHER"}""", Outcome = "Mocked" }
        };

        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.ToolCalled,
                Target = "get_work_order",
                ArgsMatchJson = """{"woNum":"B1"}"""
            },
            Context(calls: calls));

        Assert.False(result.Passed);
    }

    [Fact]
    public void Tool_not_called_passes_when_the_tool_never_appears()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.ToolNotCalled, Target = "send_text_message" },
            Context(calls: [new ObservedToolCall { FunctionName = "get_work_order", Outcome = "Mocked" }]));

        Assert.True(result.Passed);
    }

    [Fact]
    public void Tool_not_called_counts_a_blocked_call_as_called()
    {
        // Being blocked proves the agent did try to call it, which is precisely the behaviour
        // toolNotCalled exists to catch.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.ToolNotCalled, Target = "send_text_message" },
            Context(calls: [new ObservedToolCall { FunctionName = "send_text_message", Outcome = "Blocked" }]));

        Assert.False(result.Passed);
    }

    [Fact]
    public void State_equals_distinguishes_a_wrong_value_from_a_missing_key()
    {
        var wrong = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_id", Expected = "123" },
            Context(states: new Dictionary<string, string?> { ["wo_id"] = "456" }));
        Assert.False(wrong.Passed);
        Assert.Equal("456", wrong.Actual);
        Assert.Contains("differs", wrong.Message!);

        var missing = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.StateEquals, Target = "wo_id", Expected = "123" },
            Context(states: new Dictionary<string, string?>()));
        Assert.False(missing.Passed);
        Assert.Contains("not set", missing.Message!);
    }

    [Fact]
    public void Routed_to_agent_compares_case_insensitively()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "Work Order Creator" },
            Context(routedTo: "work order creator"));

        Assert.True(result.Passed);
    }

    [Fact]
    public void An_unknown_assertion_type_fails_loudly()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = "outputLooksNice" },
            Context(output: "hi"));

        Assert.False(result.Passed);
        Assert.Contains("unknown assertion type", result.Message!);
    }

    [Fact]
    public void Llm_judge_fails_loudly_here_rather_than_silently_passing()
    {
        // llmJudge is scored by IAgentTestJudge, because it needs a model call and this evaluator is
        // pure and synchronous. Reaching this branch means somebody evaluated assertions without
        // going through the runner, and the verdict they get back must be one they cannot mistake
        // for a pass -- a silent pass would show a case that verified nothing as green.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "confirms the address before quoting", MinScore = 4 },
            Context(output: "whatever"));

        Assert.False(result.Passed);
        Assert.Contains("IAgentTestJudge", result.Message!);
    }

    /// <summary>
    /// System.Text.Json.Nodes.JsonObject materialises its key/value dictionary lazily: JsonNode.Parse
    /// does not complain about duplicate top-level keys ("woNum" appearing twice), and the
    /// ArgumentException only fires on first access (TryGetPropertyValue/foreach inside IsSubset).
    /// The ArgsJson here is the model's own arguments, so it has to go through
    /// ToolMockMatcher.ParseOrNull -- catching JsonException alone is not enough, or this assertion
    /// blows the whole case up into an infrastructure Error instead of evaluating to an ordinary
    /// failure.
    /// </summary>
    [Fact]
    public void Tool_called_fails_instead_of_throwing_when_the_actual_args_have_a_duplicate_top_level_key()
    {
        var calls = new List<ObservedToolCall>
        {
            new()
            {
                FunctionName = "get_work_order",
                ArgsJson = """{"woNum":"B1","woNum":"B2"}""",
                Outcome = "Mocked"
            }
        };

        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.ToolCalled,
                Target = "get_work_order",
                ArgsMatchJson = """{"woNum":"B1"}"""
            },
            Context(calls: calls));

        Assert.False(result.Passed);
    }

    /// <summary>
    /// The regression above only pressed duplicate keys on the ArgsJson side (the model's own
    /// arguments); the ArgsMatchJson side (what the test author wrote) was never covered. Production
    /// code calls ToolMockMatcher.ParseOrNull on both sides, but if someone later reverted the
    /// ArgsMatchJson side to an unguarded parse, every earlier test here would stay green and say
    /// nothing. This is the mirror case, so both sides are pinned.
    /// </summary>
    // ---- Fix wave: a blank/omitted required field must fail, never vacuously pass -----------
    // (outputContains/outputRegex/toolNotCalled/routedToAgent verified nothing without this;
    // toolCalled/stateEquals already failed safe -- see AssertionEvaluatorTests above.)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Output_contains_fails_rather_than_vacuously_passing_on_a_blank_expected(string? expected)
    {
        // Contains("") is true for any non-null string -- without the guard this would pass
        // against ANY output, verifying nothing.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputContains, Expected = expected },
            Context(output: "anything at all"));

        Assert.False(result.Passed);
        Assert.Contains("requires a non-empty", result.Message!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Output_regex_fails_rather_than_vacuously_passing_on_a_blank_pattern(string? expected)
    {
        // An empty regex pattern matches every string -- without the guard this would pass
        // against ANY output, verifying nothing.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.OutputRegex, Expected = expected },
            Context(output: "anything at all"));

        Assert.False(result.Passed);
        Assert.Contains("requires a non-empty", result.Message!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tool_not_called_fails_rather_than_vacuously_passing_on_a_blank_target(string? target)
    {
        // A null/blank Target matches no real call's FunctionName -- without the guard this would
        // report "not called" regardless of what the agent actually did, verifying nothing.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.ToolNotCalled, Target = target },
            Context(calls: [new ObservedToolCall { FunctionName = "create_work_order", Outcome = "Mocked" }]));

        Assert.False(result.Passed);
        Assert.Contains("requires a non-empty", result.Message!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Routed_to_agent_fails_rather_than_vacuously_passing_on_a_blank_expected(string? expected)
    {
        // A null Expected compares equal to a null RoutedToAgent -- without the guard a case with
        // no routing information at all (or a typo'd blank field) would silently pass.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = expected },
            Context(routedTo: null));

        Assert.False(result.Passed);
        Assert.Contains("requires a non-empty", result.Message!);
    }

    [Fact]
    public void Tool_called_fails_instead_of_throwing_when_the_expected_args_match_json_has_a_duplicate_top_level_key()
    {
        var calls = new List<ObservedToolCall>
        {
            new() { FunctionName = "get_work_order", ArgsJson = """{"woNum":"B1"}""", Outcome = "Mocked" }
        };

        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.ToolCalled,
                Target = "get_work_order",
                ArgsMatchJson = """{"woNum":"B1","woNum":"B2"}"""
            },
            Context(calls: calls));

        Assert.False(result.Passed);
        Assert.Contains("could not be parsed", result.Message!);
    }
}

/// <summary>
/// Direct unit coverage for the save-time counterpart to the runtime fixes above:
/// <see cref="AssertionValidation.Validate"/> is what AgentTestController's create/update path
/// calls before a case is ever persisted. The eight-row type-&gt;required-field mapping is
/// exercised here directly (pure function, no controller/repo plumbing needed);
/// AgentTestControllerTests additionally proves the controller actually calls it and turns a
/// non-null result into a 400.
/// </summary>
public class AssertionValidationTests
{
    [Theory]
    [InlineData(AssertionTypes.OutputContains)]
    [InlineData(AssertionTypes.OutputNotContains)]
    [InlineData(AssertionTypes.OutputRegex)]
    [InlineData(AssertionTypes.RoutedToAgent)]
    [InlineData(AssertionTypes.LlmJudge)]
    public void Rejects_a_missing_expected_for_types_that_require_it(string type)
    {
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type }));
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type, Expected = "" }));
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type, Expected = "   " }));
    }

    [Theory]
    [InlineData(AssertionTypes.OutputContains)]
    [InlineData(AssertionTypes.OutputNotContains)]
    [InlineData(AssertionTypes.OutputRegex)]
    [InlineData(AssertionTypes.RoutedToAgent)]
    [InlineData(AssertionTypes.LlmJudge)]
    public void Accepts_a_non_blank_expected_for_types_that_require_it(string type)
    {
        Assert.Null(AssertionValidation.Validate(new TestAssertion { Type = type, Expected = "x" }));
    }

    [Theory]
    [InlineData(AssertionTypes.ToolCalled)]
    [InlineData(AssertionTypes.ToolNotCalled)]
    [InlineData(AssertionTypes.StateEquals)]
    public void Rejects_a_missing_target_for_types_that_require_it(string type)
    {
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type }));
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type, Target = "" }));
        Assert.NotNull(AssertionValidation.Validate(new TestAssertion { Type = type, Target = "   " }));
    }

    [Theory]
    [InlineData(AssertionTypes.ToolCalled)]
    [InlineData(AssertionTypes.ToolNotCalled)]
    [InlineData(AssertionTypes.StateEquals)]
    public void Accepts_a_non_blank_target_for_types_that_require_it(string type)
    {
        Assert.Null(AssertionValidation.Validate(new TestAssertion { Type = type, Target = "x" }));
    }

    [Fact]
    public void Does_not_reject_an_unrecognized_type()
    {
        // Not this validator's job -- AssertionEvaluator's own `default` branch already fails an
        // unknown type loudly at evaluation time. Rejecting it here too would block saving a case
        // authored against a not-yet-released assertion type.
        Assert.Null(AssertionValidation.Validate(new TestAssertion { Type = "somethingNotYetSupported" }));
    }
}
