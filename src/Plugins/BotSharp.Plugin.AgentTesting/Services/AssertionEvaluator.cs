using System.Text.RegularExpressions;
using BotSharp.Plugin.AgentTesting.Runtime;

namespace BotSharp.Plugin.AgentTesting.Services;

public static class AssertionTypes
{
    public const string OutputContains = "outputContains";
    public const string OutputNotContains = "outputNotContains";
    public const string OutputRegex = "outputRegex";
    public const string ToolCalled = "toolCalled";
    public const string ToolNotCalled = "toolNotCalled";
    public const string StateEquals = "stateEquals";
    public const string RoutedToAgent = "routedToAgent";
    public const string LlmJudge = "llmJudge";
}

/// <summary>
/// 一条断言的求值必须是纯函数：同样的 (assertion, context) 永远给出同样的
/// AssertionResult，不做 I/O、不依赖任何服务。Runner（Task 7）按轮调一次，
/// 整案断言跑完全部轮后再调一次——这正是它是"红绿灯"定义的原因：可复现、可解释。
/// </summary>
public static class AssertionEvaluator
{
    public static AssertionResult Evaluate(TestAssertion assertion, AssertionContext context)
    {
        var result = new AssertionResult
        {
            Type = assertion.Type,
            Target = assertion.Target,
            Expected = assertion.Expected
        };

        switch (assertion.Type)
        {
            case AssertionTypes.OutputContains:
                result.Actual = context.Output;
                if (string.IsNullOrEmpty(assertion.Expected))
                {
                    // Contains("") is vacuously true for any non-null string -- a blank/omitted
                    // Expected must not read as "the output contains nothing," which verifies
                    // nothing and would always report Passed.
                    result.Passed = false;
                    result.Message = "outputContains requires a non-empty 'expected' value";
                }
                else
                {
                    result.Passed = context.Output?.Contains(assertion.Expected,
                        StringComparison.OrdinalIgnoreCase) == true;
                    if (!result.Passed) result.Message = "output does not contain the expected text";
                }
                break;

            case AssertionTypes.OutputNotContains:
                result.Actual = context.Output;
                result.Passed = context.Output?.Contains(assertion.Expected ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase) != true;
                if (!result.Passed) result.Message = "output contains text that should not appear";
                break;

            case AssertionTypes.OutputRegex:
                result.Actual = context.Output;
                if (string.IsNullOrEmpty(assertion.Expected))
                {
                    // An empty pattern matches everything -- a blank/omitted Expected must not
                    // read as "match anything," which verifies nothing and would always pass.
                    result.Passed = false;
                    result.Message = "outputRegex requires a non-empty 'expected' pattern";
                    break;
                }
                try
                {
                    result.Passed = Regex.IsMatch(context.Output ?? string.Empty, assertion.Expected,
                        RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    if (!result.Passed) result.Message = "output does not match the pattern";
                }
                catch (ArgumentException)
                {
                    // 正则是用户输入，写错是常态。判失败并说清原因，不要让整个用例记成基础设施错误。
                    result.Passed = false;
                    result.Message = $"invalid regular expression: {assertion.Expected}";
                }
                catch (RegexMatchTimeoutException)
                {
                    result.Passed = false;
                    result.Message = "regular expression timed out";
                }
                break;

            case AssertionTypes.ToolCalled:
            {
                var matches = context.ToolCalls
                    .Where(c => string.Equals(c.FunctionName, assertion.Target, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                result.Actual = string.Join(", ", matches.Select(m => m.ArgsJson ?? "{}"));

                if (matches.Count == 0)
                {
                    result.Passed = false;
                    result.Message = "the tool was never called";
                }
                else if (string.IsNullOrWhiteSpace(assertion.ArgsMatchJson))
                {
                    result.Passed = true;
                }
                else
                {
                    // ArgsMatchJson 来自测试作者，ArgsJson 来自模型输出——两边都可能是空白、
                    // 语法非法，或语法合法但含顶层重复键（JsonObject 的字典是惰性物化的，
                    // 直到 IsSubset 里第一次访问才抛 ArgumentException）。ParseOrNull 已经把
                    // 这整套失败模式统一收敛成"返回 null"，这里不重新写一遍解析。
                    var expected = ToolMockMatcher.ParseOrNull(assertion.ArgsMatchJson);
                    if (expected == null)
                    {
                        result.Passed = false;
                        result.Message = "argument json could not be parsed";
                    }
                    else
                    {
                        result.Passed = matches.Any(m =>
                        {
                            var actual = ToolMockMatcher.ParseOrNull(m.ArgsJson);
                            return actual != null && ToolMockMatcher.IsSubset(expected, actual);
                        });
                        if (!result.Passed) result.Message = "the tool was called with different arguments";
                    }
                }
                break;
            }

            case AssertionTypes.ToolNotCalled:
            {
                if (string.IsNullOrWhiteSpace(assertion.Target))
                {
                    // A null/blank Target matches no real call's FunctionName, so this would
                    // otherwise always report "not called" -- vacuously passing without ever
                    // naming a tool to check.
                    result.Passed = false;
                    result.Message = "toolNotCalled requires a non-empty 'target' function name";
                    break;
                }

                // 被阻断也算"调用过"——agent 确实想调它，这正是要抓的行为。
                var called = context.ToolCalls
                    .Any(c => string.Equals(c.FunctionName, assertion.Target, StringComparison.OrdinalIgnoreCase));
                result.Passed = !called;
                result.Actual = called ? "called" : "not called";
                if (!result.Passed) result.Message = "the tool should not have been called";
                break;
            }

            case AssertionTypes.StateEquals:
                if (assertion.Target != null && context.States.TryGetValue(assertion.Target, out var value))
                {
                    result.Actual = value;
                    result.Passed = string.Equals(value, assertion.Expected, StringComparison.Ordinal);
                    if (!result.Passed) result.Message = "state value differs from the expected value";
                }
                else
                {
                    result.Passed = false;
                    result.Message = $"state '{assertion.Target}' is not set";
                }
                break;

            case AssertionTypes.RoutedToAgent:
                result.Actual = context.RoutedToAgent;
                if (string.IsNullOrWhiteSpace(assertion.Expected))
                {
                    // A null Expected compares equal to a null RoutedToAgent (e.g. the canary/no
                    // routing information case) -- a blank/omitted Expected must not read as
                    // "expect no routing," which verifies nothing and would always pass.
                    result.Passed = false;
                    result.Message = "routedToAgent requires a non-empty 'expected' agent name";
                    break;
                }

                result.Passed = string.Equals(context.RoutedToAgent, assertion.Expected,
                    StringComparison.OrdinalIgnoreCase);
                if (!result.Passed) result.Message = "the conversation was handled by a different agent";
                break;

            case AssertionTypes.LlmJudge:
                // P2 接 IInstructService 判官。P1 显式失败，绝不静默通过——
                // 静默通过会让一条什么都没验证的用例显示为绿色。
                result.Passed = false;
                result.Message = "llmJudge is not available in P1";
                break;

            default:
                result.Passed = false;
                result.Message = $"unknown assertion type '{assertion.Type}'";
                break;
        }

        return result;
    }
}

/// <summary>
/// Save-time counterpart to the four fixed <see cref="AssertionEvaluator"/> branches above
/// (outputContains/outputRegex/toolNotCalled/routedToAgent): an assertion missing the one field
/// its type actually needs to verify anything is rejected at case create/update
/// (AgentTestController), not just left to fail at run time. toolCalled/stateEquals already
/// fail safe on a null Target at evaluation time; they get the same save-time guard here for
/// consistency, so a typo'd blank field is caught at authoring time for every assertion type,
/// not only the four that would otherwise vacuously pass.
/// </summary>
public static class AssertionValidation
{
    private enum RequiredField { Expected, Target }

    // One row per AssertionTypes constant -- eight total.
    private static readonly Dictionary<string, RequiredField> Requirements = new(StringComparer.Ordinal)
    {
        [AssertionTypes.OutputContains] = RequiredField.Expected,
        [AssertionTypes.OutputNotContains] = RequiredField.Expected,
        [AssertionTypes.OutputRegex] = RequiredField.Expected,
        [AssertionTypes.ToolCalled] = RequiredField.Target,
        [AssertionTypes.ToolNotCalled] = RequiredField.Target,
        [AssertionTypes.StateEquals] = RequiredField.Target,
        [AssertionTypes.RoutedToAgent] = RequiredField.Expected,
        [AssertionTypes.LlmJudge] = RequiredField.Expected,
    };

    /// <summary>Null when the assertion is well-formed; otherwise a caller-facing error message.</summary>
    public static string? Validate(TestAssertion assertion)
    {
        // An unrecognized type is not this method's job to reject -- AssertionEvaluator's own
        // `default` branch already fails it loudly at evaluation time, and rejecting it here too
        // would block saving a case authored against a not-yet-released assertion type.
        if (!Requirements.TryGetValue(assertion.Type, out var required))
        {
            return null;
        }

        return required switch
        {
            RequiredField.Expected when string.IsNullOrWhiteSpace(assertion.Expected)
                => $"assertion '{assertion.Type}' requires a non-empty 'expected' value",
            RequiredField.Target when string.IsNullOrWhiteSpace(assertion.Target)
                => $"assertion '{assertion.Type}' requires a non-empty 'target' value",
            _ => null
        };
    }
}
