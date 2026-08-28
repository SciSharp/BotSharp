namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Works out which cases a given change actually needs to run.
///
/// A model or prompt change does not justify running everything: the cost is real and mostly buys
/// nothing, since a change to one agent cannot affect a case that never touches it. But the reverse
/// error is far worse -- a case wrongly left out reports nothing at all, and "not run" is
/// indistinguishable from "passed" once the numbers are in a report. So every rule here resolves
/// towards including, and the one thing the caller must never be able to do is quietly shrink the
/// scope without it showing up in the excluded list.
///
/// A pure function, like <see cref="AssertionEvaluator"/>: the same (case, suite agent, query) always
/// yields the same decision with no I/O, which is what makes the rules exhaustively testable rather
/// than something to be argued about.
/// </summary>
public static class CaseScope
{
    /// <summary>
    /// The agents a case exercises, as ids.
    ///
    /// An authored <see cref="AgentTestCase.InvolvedAgents"/> wins. Otherwise it falls back to the
    /// case's entry agent, which is already known and is definitionally involved -- so an Agent case
    /// is correctly picked up by a change to the agent it runs against without anyone maintaining a
    /// list.
    ///
    /// That fallback is exactly right for an Agent case and only a starting point for a Routing case,
    /// where the entry agent is the router and the agents that matter are downstream of it. Those
    /// cannot be derived from the case definition -- they are only visible once it has run -- which is
    /// what authoring InvolvedAgents is for.
    /// </summary>
    public static IReadOnlyList<string> InvolvedAgentIds(AgentTestCase testCase, string? suiteAgentId)
    {
        if (testCase.InvolvedAgents.Count > 0)
        {
            return testCase.InvolvedAgents
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .ToList();
        }

        var entry = string.IsNullOrWhiteSpace(testCase.EntryAgentId) ? suiteAgentId : testCase.EntryAgentId;
        return string.IsNullOrWhiteSpace(entry) ? [] : [entry.Trim()];
    }

    /// <summary>
    /// Whether this case belongs in the scope, and which rule decided it. Rules are applied in the
    /// order below and the first match wins.
    /// </summary>
    public static ScopeDecision Decide(AgentTestCase testCase, string? suiteAgentId, ScopeQuery query)
    {
        var involved = InvolvedAgentIds(testCase, suiteAgentId);
        var batch = CaseBatches.Effective(testCase);

        // Before the rules: a disabled case is skipped by the executor, so calling it "in scope"
        // would overstate coverage by exactly the cases nobody is running. Reported as excluded with
        // its own reason so a disabled cross-cutting safety case stands out rather than blending into
        // the cases a change genuinely cannot affect.
        if (!testCase.Enabled)
        {
            return new ScopeDecision(false, ScopeReasons.Disabled, involved, batch);
        }

        // A separate axis from the rules below: batches exist to run in order and stop early, so
        // narrowing to one batch is scheduling rather than scoping. Null means every batch.
        if (query.Batch is { } wanted && batch != wanted)
        {
            return new ScopeDecision(false, ScopeReasons.OtherBatch, involved, batch);
        }

        // Rule 1. Cross-cutting cases run in every scope, whatever changed. The whole point of
        // narrowing is to skip cases a change cannot affect, and "this change cannot affect safety"
        // is precisely the claim not to accept without checking.
        if (testCase.CrossCutting)
        {
            return new ScopeDecision(true, ScopeReasons.CrossCutting, involved, batch);
        }

        // Rule 2. A platform-wide change -- foundation model, provider swap, infrastructure -- turns
        // narrowing off entirely, because there is no agent it demonstrably does not touch.
        if (query.FullPlatform)
        {
            return new ScopeDecision(true, ScopeReasons.FullPlatform, involved, batch);
        }

        // Rule 3. The case touches at least one of the changed agents.
        if (involved.Any(id => query.TargetAgentIds.Any(
                target => string.Equals(id, target, StringComparison.OrdinalIgnoreCase))))
        {
            return new ScopeDecision(true, ScopeReasons.TargetAgent, involved, batch);
        }

        // Rule 4. Nothing matched. Note this is only reachable when the case HAS a known involved
        // set: with neither an authored list nor an entry agent the set is empty, which is handled
        // below rather than falling through to an exclusion.
        if (involved.Count == 0)
        {
            // Fail open. An unknown involved set means the harness cannot show the change does not
            // affect this case, and a wrongly excluded case is silent -- it produces no result to
            // notice. Running one case more than necessary costs tokens; skipping one hides a
            // regression.
            return new ScopeDecision(true, ScopeReasons.UnknownAgents, involved, batch);
        }

        return new ScopeDecision(false, ScopeReasons.NotInvolved, involved, batch);
    }
}

/// <summary>What the caller says changed.</summary>
public class ScopeQuery
{
    /// <summary>Agent ids the change touches. Ignored when <see cref="FullPlatform"/> is set.</summary>
    public IReadOnlyList<string> TargetAgentIds { get; set; } = [];

    /// <summary>A platform-wide change: narrowing is switched off and every enabled case is in.</summary>
    public bool FullPlatform { get; set; }

    /// <summary>Narrow to one batch; null covers all of them.</summary>
    public int? Batch { get; set; }
}

/// <summary>Why one case is in or out.</summary>
public class ScopeDecision
{
    public ScopeDecision(bool included, string reason, IReadOnlyList<string> involvedAgentIds, int batch)
    {
        Included = included;
        Reason = reason;
        InvolvedAgentIds = involvedAgentIds;
        Batch = batch;
    }

    public bool Included { get; }

    /// <summary>See <see cref="ScopeReasons"/>. Always populated, for excluded cases too.</summary>
    public string Reason { get; }

    /// <summary>The set the decision was actually made against, authored or derived.</summary>
    public IReadOnlyList<string> InvolvedAgentIds { get; }

    /// <summary>The effective batch, after the priority and cross-cutting derivation.</summary>
    public int Batch { get; }
}

/// <summary>
/// Which rule decided a case. Reported per case rather than only as a count, because a scope nobody
/// can explain is a scope nobody can review -- and reviewing it is the only defence against a change
/// being signed off against a set of cases that quietly excluded the interesting one.
/// </summary>
public static class ScopeReasons
{
    /// <summary>Included: cross-cutting, so it runs in every scope.</summary>
    public const string CrossCutting = "crossCutting";

    /// <summary>Included: the change is platform-wide, so narrowing is off.</summary>
    public const string FullPlatform = "fullPlatform";

    /// <summary>Included: the case exercises one of the changed agents.</summary>
    public const string TargetAgent = "targetAgent";

    /// <summary>Included: no involved agents are known, so it cannot be shown to be unaffected.</summary>
    public const string UnknownAgents = "unknownAgents";

    /// <summary>Excluded: the case does not touch any changed agent.</summary>
    public const string NotInvolved = "notInvolved";

    /// <summary>Excluded: the case is disabled, so no run would execute it.</summary>
    public const string Disabled = "disabled";

    /// <summary>Excluded: the case belongs to a different batch than the one asked for.</summary>
    public const string OtherBatch = "otherBatch";
}
