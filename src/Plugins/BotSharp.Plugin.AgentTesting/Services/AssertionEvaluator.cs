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

    /// <summary>
    /// Result-only. Never authored on a case and never evaluated -- AgentTestCaseRunner synthesises
    /// it when the mock seam blocked a tool, so the block surfaces in the ordinary assertion table
    /// instead of only in Observed Tool Calls. Deliberately absent from AssertionValidation's
    /// Requirements map, which covers the eight authorable types.
    /// </summary>
    public const string NoBlockedTools = "noBlockedTools";
}

/// <summary>
/// Evaluating an assertion is a pure function: the same (assertion, context) always yields the same
/// AssertionResult, with no I/O and no service dependencies. The runner calls it once per turn, and
/// again for case-level assertions after every turn has run. That purity is exactly what makes it
/// usable as the pass/fail verdict -- reproducible and explainable.
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
                    // The regex is user input and being malformed is routine. Fail the assertion
                    // and say why, rather than recording the whole case as an infrastructure error.
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
                    // ArgsMatchJson comes from the test author and ArgsJson from model output --
                    // either can be blank, syntactically invalid, or syntactically valid but with
                    // duplicate top-level keys (JsonObject's dictionary materialises lazily, so the
                    // ArgumentException only fires on first access inside IsSubset). ParseOrNull
                    // already collapses that whole family of failures into "returns null", so the
                    // parsing is not rewritten here.
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

                // A blocked call still counts as "called": the agent did try to call it, and that
                // attempt is exactly the behaviour being asserted on.
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
                // P2 will wire this to an IInstructService judge. P1 fails explicitly and never
                // passes silently -- passing silently would show a case that verified nothing as
                // green.
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
