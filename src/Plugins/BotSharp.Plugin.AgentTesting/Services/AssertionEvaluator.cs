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
    public const string AgentChain = "agentChain";
    public const string LlmJudge = "llmJudge";

    /// <summary>
    /// Result-only. Never authored on a case and never evaluated -- AgentTestCaseRunner synthesises
    /// it when the mock seam blocked a tool, so the block surfaces in the ordinary assertion table
    /// instead of only in Observed Tool Calls. Deliberately absent from AssertionValidation's
    /// Requirements map, which covers the nine authorable types.
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
                var lastHop = context.AgentChain.Count > 0 ? context.AgentChain[^1] : null;
                result.Actual = lastHop?.Name;
                if (string.IsNullOrWhiteSpace(assertion.Expected))
                {
                    // A blank Expected would compare equal to "no agent answered", which verifies
                    // nothing and would always pass.
                    result.Passed = false;
                    result.Message = "routedToAgent requires a non-empty 'expected' agent name or id";
                    break;
                }

                // Either identifier is accepted -- see AgentChainHop.Matches.
                result.Passed = lastHop?.Matches(assertion.Expected.Trim()) == true;
                if (!result.Passed) result.Message = "the conversation was handled by a different agent";
                break;

            case AssertionTypes.AgentChain:
                EvaluateAgentChain(assertion, context, result);
                break;

            case AssertionTypes.LlmJudge:
                // Unreachable on the normal path: the runner routes llmJudge to IAgentTestJudge
                // instead of here, because scoring it needs a model call and this method is a pure,
                // synchronous, I/O-free function. Kept as a loud failure rather than removed, so
                // that a caller who evaluates assertions without going through the runner gets a
                // verdict it cannot mistake for a pass. Silently passing would show a case that
                // verified nothing as green.
                result.Passed = false;
                result.Message = "llmJudge must be evaluated through IAgentTestJudge, not AssertionEvaluator";
                break;

            default:
                result.Passed = false;
                result.Message = $"unknown assertion type '{assertion.Type}'";
                break;
        }

        return result;
    }

    /// <summary>
    /// Compares the agents that answered against an expected list. Complements routedToAgent rather
    /// than replacing it: routedToAgent asks "who answered last", which cannot express a hand-off at
    /// all -- in Entry -> A -> B only B is visible, and if control returns to the entry agent and it
    /// emits the closing message, a correctly routed case reads as routed to the entry agent.
    ///
    /// <see cref="TestAssertion.Expected"/> is a comma-separated list of agent names.
    /// <see cref="TestAssertion.Target"/> selects the mode -- see <see cref="AgentChainModes"/> --
    /// and defaults to Contains when omitted.
    /// </summary>
    private static void EvaluateAgentChain(
        TestAssertion assertion, AssertionContext context, AssertionResult result)
    {
        result.Actual = string.Join(" -> ", context.AgentChain.Select(hop => hop.Name));

        var expected = SplitAgentNames(assertion.Expected);
        if (expected.Count == 0)
        {
            // An empty expected list is a subset of, and an ordered subsequence of, any chain, so
            // both Contains and Ordered would pass vacuously; Exact would silently assert "no agent
            // ever answered", which no author means to write.
            result.Passed = false;
            result.Message = "agentChain requires a non-empty comma-separated 'expected' agent list";
            return;
        }

        // Deliberately not defaulted on a typo: falling back to Contains for an unrecognised mode
        // would turn "orderd" into the loosest available check and quietly verify much less than the
        // author asked for.
        var mode = AgentChainModes.Normalize(assertion.Target);
        if (mode == null)
        {
            result.Passed = false;
            result.Message = "agentChain 'target' must be one of "
                           + string.Join(", ", AgentChainModes.All)
                           + " (or empty for " + AgentChainModes.Contains + "), not '"
                           + assertion.Target + "'";
            return;
        }

        switch (mode)
        {
            case AgentChainModes.Exact:
                result.Passed = context.AgentChain.Count == expected.Count
                    && context.AgentChain.Zip(expected).All(pair => pair.First.Matches(pair.Second));
                if (!result.Passed) result.Message = "the agent chain differs from the expected chain";
                break;

            case AgentChainModes.Ordered:
                result.Passed = IsOrderedSubsequence(expected, context.AgentChain);
                if (!result.Passed) result.Message = "the expected agents did not all appear, in that order";
                break;

            default:
                var missing = expected
                    .Where(e => !context.AgentChain.Any(hop => hop.Matches(e)))
                    .ToList();
                result.Passed = missing.Count == 0;
                if (!result.Passed)
                {
                    result.Message = "the agent chain does not include " + string.Join(", ", missing);
                }
                break;
        }
    }

    /// <summary>
    /// Whether every expected name appears in the chain in the given relative order, with other
    /// agents allowed in between -- so ["Copilot", "WorkOrder"] matches
    /// Copilot -> Diagnosis -> WorkOrder. Asserting the hand-offs an author cares about must not
    /// require enumerating every agent the conversation happened to pass through; Exact is for that.
    /// </summary>
    private static bool IsOrderedSubsequence(List<string> expected, IReadOnlyList<AgentChainHop> chain)
    {
        var next = 0;
        foreach (var hop in chain)
        {
            if (next < expected.Count && hop.Matches(expected[next]))
            {
                next++;
            }
        }

        return next == expected.Count;
    }

    private static List<string> SplitAgentNames(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}

/// <summary>
/// How an agentChain assertion compares its expected list against the actual chain.
///
/// Contains -- every expected agent appears somewhere, order ignored. The default, and the right
///             choice for "this agent must have been involved at all".
/// Ordered  -- every expected agent appears, in that relative order, other agents allowed in
///             between. This is the hand-off assertion.
/// Exact    -- the chain is precisely the expected list and nothing else. Strictest, and also how to
///             assert isolation: an Exact chain of one agent means nothing routed away.
/// </summary>
public static class AgentChainModes
{
    public const string Contains = "contains";
    public const string Ordered = "ordered";
    public const string Exact = "exact";

    public static readonly string[] All = [Contains, Ordered, Exact];

    /// <summary>
    /// Canonical mode for any casing, with blank meaning <see cref="Contains"/>; null for an
    /// unrecognised value, so the caller rejects it instead of guessing.
    /// </summary>
    public static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? Contains
            : All.FirstOrDefault(m => string.Equals(m, value.Trim(), StringComparison.OrdinalIgnoreCase));
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

    // One row per authorable AssertionTypes constant -- nine total.
    private static readonly Dictionary<string, RequiredField> Requirements = new(StringComparer.Ordinal)
    {
        [AssertionTypes.OutputContains] = RequiredField.Expected,
        [AssertionTypes.OutputNotContains] = RequiredField.Expected,
        [AssertionTypes.OutputRegex] = RequiredField.Expected,
        [AssertionTypes.ToolCalled] = RequiredField.Target,
        [AssertionTypes.ToolNotCalled] = RequiredField.Target,
        [AssertionTypes.StateEquals] = RequiredField.Target,
        [AssertionTypes.RoutedToAgent] = RequiredField.Expected,
        [AssertionTypes.AgentChain] = RequiredField.Expected,
        [AssertionTypes.LlmJudge] = RequiredField.Expected,
    };

    /// <summary>
    /// The nine authorable types, in the order the Requirements map above declares them.
    ///
    /// Exposed so that ICaseAuthor can generate the assertion vocabulary it puts in front of a model
    /// from this map rather than from a hand-written copy in a prompt string. A prompt that lists a
    /// type this map does not know, or omits one it does, produces drafts that fail validation for
    /// reasons the author cannot see.
    /// </summary>
    public static IReadOnlyList<string> Authorable { get; } = Requirements.Keys.ToArray();

    /// <summary>
    /// Which field this assertion type must carry: "expected", "target", or null for a type with no
    /// such requirement.
    /// </summary>
    public static string? RequiredFieldName(string type)
        => Requirements.TryGetValue(type, out var required)
            ? required == RequiredField.Expected ? "expected" : "target"
            : null;

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
