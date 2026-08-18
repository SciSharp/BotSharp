using System.Collections.Generic;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// 断言求值是红绿灯的定义，必须纯函数、可穷举。这里逐类型钉住，包括几个容易写错的边界：
/// 正则非法不能把用例炸成 Error（用户会写错正则）、toolCalled 的入参匹配是子集而非全等
/// （否则用户得把模型传的每个参数都列全）、stateEquals 区分"值不等"和"key 根本不存在"。
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
            RoutedToAgent = routedTo
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
        // 被阻断说明 agent 确实想调它，这正是 toolNotCalled 要抓的行为。
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
    public void Llm_judge_is_reported_as_unavailable_in_p1_rather_than_silently_passing()
    {
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "应先确认地址再报价", MinScore = 0.8 },
            Context(output: "whatever"));

        Assert.False(result.Passed);
        Assert.Contains("not available in P1", result.Message!);
    }

    /// <summary>
    /// System.Text.Json.Nodes.JsonObject 的键值字典是惰性物化的：JsonNode.Parse 对顶层重复键
    /// （"woNum" 出现两次）不报错，直到第一次访问（IsSubset 内部的 TryGetPropertyValue/foreach）
    /// 才抛 ArgumentException。这里的 ArgsJson 是模型产出的实参，必须走
    /// ToolMockMatcher.ParseOrNull（同 Task 5 里已经修过的那处），不能只捕获 JsonException——
    /// 否则这条断言会把整个用例炸成基础设施 Error，而不是求值成一次普通的失败。
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
    /// 上一条回归测试只在 ArgsJson（模型产出的实参）那一侧压过重复键；ArgsMatchJson
    /// （测试作者自己写的期望值）那一侧从未被覆盖。生产代码在两侧都调用
    /// ToolMockMatcher.ParseOrNull（AssertionEvaluator.cs:93 和 :103），但如果未来有人把
    /// ArgsMatchJson 那一侧改回不带防护的裸解析，之前的 13 个测试会全绿、毫无警觉——
    /// 这里补上镜像的另一侧，把两侧都钉住。
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
