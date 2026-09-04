namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Shared create/update validation for a case payload, lifted out of AgentTestController so that
/// every path which produces a case runs the same rules.
///
/// That sharing is the point, not tidiness: <see cref="ICaseAuthor"/> hands a model-authored draft
/// back to a human to save, and a draft that only fails when they press Save wastes the round trip
/// and teaches them to distrust the feature. The author service runs these rules itself and gives
/// the model one chance to repair its own output against the real error text.
///
/// Everything here is static and I/O-free. The one case rule that needs a service --
/// does the entry agent exist -- stays on the controller as ValidateEntryAgentAsync.
/// </summary>
public static class CaseValidation
{
    /// <summary>
    /// Shared create/update validation for a case payload. Null means the payload is acceptable;
    /// otherwise the string is a caller-facing 400 message.
    /// </summary>
    public static string? Validate(AgentTestCaseUpsertRequest request)
    {
        if (IsUnsupportedUnmockedToolPolicy(request.UnmockedToolPolicy))
        {
            return "Passthrough is not supported in P1";
        }

        var caseType = CaseTypes.Normalize(request.CaseType);
        if (caseType == null && !string.IsNullOrWhiteSpace(request.CaseType))
        {
            return $"caseType must be one of {string.Join(", ", CaseTypes.All)}, not '{request.CaseType}'";
        }

        if (CasePriorities.Normalize(request.Priority) == null && !string.IsNullOrWhiteSpace(request.Priority))
        {
            return $"priority must be one of {string.Join(", ", CasePriorities.All)}, not '{request.Priority}'";
        }

        if (CaseSeverities.Normalize(request.Severity) == null && !string.IsNullOrWhiteSpace(request.Severity))
        {
            return $"severity must be one of {string.Join(", ", CaseSeverities.All)}, not '{request.Severity}'";
        }

        // Rejected rather than clamped: a batch of 4 is a mistake, and silently filing the case in
        // batch 3 would leave the author believing it runs somewhere it does not.
        if (request.Batch is { } batch && !CaseBatches.All.Contains(batch))
        {
            return $"batch must be one of {string.Join(", ", CaseBatches.All)}, not {batch}";
        }

        foreach (var (message, index) in (request.History ?? []).Select((m, i) => (m, i)))
        {
            if (HistoryRoles.Normalize(message?.Role) == null)
            {
                return $"history message {index + 1} has role '{message?.Role}'; only "
                     + $"{string.Join(" and ", HistoryRoles.All)} are supported";
            }

            // An empty message is dropped by BotSharp's own dialog storage (ConversationStorage
            // skips elements with blank content), so it would silently not be there at run time --
            // and the runner's count check would then fail the whole case with a confusing message
            // about the write having vanished.
            if (string.IsNullOrWhiteSpace(message!.Content))
            {
                return $"history message {index + 1} has no content";
            }
        }

        var allAssertions = (request.Turns ?? [])
            .SelectMany(t => t.Assertions ?? [])
            .Concat(request.Assertions ?? [])
            .ToList();

        foreach (var assertion in allAssertions)
        {
            var error = AssertionValidation.Validate(assertion);
            if (error != null)
            {
                return error;
            }
        }

        return ValidateRoutingCase(caseType ?? CaseTypes.Agent, request, allAssertions);
    }

    /// <summary>
    /// The extra rules a Routing case has to satisfy. A Routing case is not a label -- it is the only
    /// type counted towards a run's routing accuracy, so one that cannot actually establish a routing
    /// outcome would quietly move that figure without measuring anything.
    ///
    /// Enforced at save time for the same reason AssertionValidation is: the alternative is a case
    /// that saves cleanly and then reports a meaningless green every run.
    /// </summary>
    private static string? ValidateRoutingCase(
        string caseType, AgentTestCaseUpsertRequest request, List<TestAssertion> allAssertions)
    {
        if (!string.Equals(caseType, CaseTypes.Routing, StringComparison.Ordinal))
        {
            return null;
        }

        // Routing is a single-turn question: which agent picks this message up. A second turn is
        // either a different question (making it an Agent case) or an accident, and either way its
        // result would be counted as routing accuracy.
        //
        // Authored History is not a turn and is deliberately not counted here: replaying a prior
        // exchange and then asking one question is still a single routing decision, and it is the
        // most realistic way to test routing that depends on context.
        if ((request.Turns ?? []).Count != 1)
        {
            return "a Routing case must have exactly one turn; use an Agent case for a multi-turn case";
        }

        // Without one of these the case asserts nothing about routing, yet still counts towards
        // routing accuracy -- it would report Passed for having successfully said anything at all.
        var assertsRouting = allAssertions.Any(a =>
            string.Equals(a.Type, AssertionTypes.RoutedToAgent, StringComparison.Ordinal)
            || string.Equals(a.Type, AssertionTypes.AgentChain, StringComparison.Ordinal));
        if (!assertsRouting)
        {
            return $"a Routing case needs at least one '{AssertionTypes.RoutedToAgent}' or "
                 + $"'{AssertionTypes.AgentChain}' assertion, otherwise it verifies no routing outcome";
        }

        // The framework this implements scores routing purely as expected-agent == actual-agent and
        // deliberately applies no quality judgement to it. An llmJudge here would also make the
        // routing figure depend on a vendor call, so a vendor outage would read as a routing
        // regression.
        if (allAssertions.Any(a => string.Equals(a.Type, AssertionTypes.LlmJudge, StringComparison.Ordinal)))
        {
            return $"a Routing case cannot use '{AssertionTypes.LlmJudge}': routing is judged only by "
                 + "which agent handled the conversation";
        }

        return null;
    }

    /// <summary>
    /// Passthrough was specified in the design/plan and even had a (dead) code path, but nothing
    /// ever back-fills an ObservedToolCall for a tool the provider let run for real -- under it,
    /// toolNotCalled always vacuously passed against a tool that genuinely executed with real side
    /// effects. Rejected here rather than implementing the back-fill (project owner decision).
    /// </summary>
    private static bool IsUnsupportedUnmockedToolPolicy(string? policy)
        => string.Equals(policy, "Passthrough", StringComparison.OrdinalIgnoreCase);
}
