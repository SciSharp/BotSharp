using System.Collections.Generic;
using System.Linq;
using BotSharp.Plugin.AgentTesting.Models;
using BotSharp.Plugin.AgentTesting.Services;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// Scope narrowing is the one feature here whose failure mode is silence. A case wrongly INCLUDED
/// costs tokens and is obvious. A case wrongly EXCLUDED produces no result at all, and once the
/// numbers reach a report "not run" is indistinguishable from "passed" -- so every rule below is
/// checked in the direction of including, and the exclusions are checked for having a reason someone
/// can review.
/// </summary>
public class CaseScopeTests
{
    private const string CopilotId = "2cd4b805-7078-4405-87e9-2ec9aadf8a11";
    private const string WoCancellationId = "0fe3905d-75f1-4e2e-8e54-ec3d33d6b6f0";
    private const string DiagnosisId = "11111111-2222-3333-4444-555555555555";

    private static AgentTestCase Case(
        string? entryAgentId = null,
        bool enabled = true,
        bool crossCutting = false,
        string priority = CasePriorities.P1,
        int? batch = null,
        params string[] involved) => new()
    {
        Id = "case-1",
        SuiteId = "suite-1",
        Name = "c",
        Enabled = enabled,
        CrossCutting = crossCutting,
        Priority = priority,
        Batch = batch,
        EntryAgentId = entryAgentId,
        InvolvedAgents = involved.ToList(),
        Turns = [new TestTurn { Index = 0, UserMessage = "hi" }]
    };

    private static ScopeQuery Changed(params string[] targetAgentIds)
        => new() { TargetAgentIds = targetAgentIds };

    // ------------------------------------------------------------------ involved agents

    [Fact]
    public void An_authored_involved_list_wins_over_the_entry_agent()
    {
        // The reason the field exists: for a routing case the entry agent is the router, and the
        // agents that matter are the ones downstream of it. Those cannot be derived from the case
        // definition at all.
        var involved = CaseScope.InvolvedAgentIds(
            Case(entryAgentId: CopilotId, involved: [WoCancellationId, DiagnosisId]), "suite-agent");

        Assert.Equal([WoCancellationId, DiagnosisId], involved);
    }

    [Fact]
    public void With_no_authored_list_the_entry_agent_is_the_involved_agent()
    {
        // Definitionally true and already known, so an Agent case is picked up by a change to the
        // agent it runs against without anyone maintaining a list.
        Assert.Equal([CopilotId], CaseScope.InvolvedAgentIds(Case(entryAgentId: CopilotId), "suite-agent"));
    }

    [Fact]
    public void With_no_entry_agent_the_suites_agent_is_used()
    {
        // Which is exactly what the runner does when it opens the conversation, so the scope is
        // computed against the agent the case really runs on.
        Assert.Equal(["suite-agent"], CaseScope.InvolvedAgentIds(Case(), "suite-agent"));
    }

    [Fact]
    public void Blank_entries_in_an_authored_list_are_ignored()
    {
        // A UI that posts an empty row must not contribute an agent id of "" that matches nothing and
        // silently narrows the scope.
        var involved = CaseScope.InvolvedAgentIds(
            Case(involved: [CopilotId, "", "   "]), "suite-agent");

        Assert.Equal([CopilotId], involved);
    }

    // ------------------------------------------------------------------ rules

    [Fact]
    public void Rule_3_includes_a_case_that_touches_a_changed_agent()
    {
        var decision = CaseScope.Decide(Case(entryAgentId: CopilotId), "suite-agent", Changed(CopilotId));

        Assert.True(decision.Included);
        Assert.Equal(ScopeReasons.TargetAgent, decision.Reason);
    }

    [Fact]
    public void Rule_4_excludes_a_case_that_touches_none_of_them()
    {
        // The entire point of narrowing: a change to one agent cannot affect a case that never goes
        // near it, and running it anyway buys nothing.
        var decision = CaseScope.Decide(Case(entryAgentId: CopilotId), "suite-agent", Changed(DiagnosisId));

        Assert.False(decision.Included);
        Assert.Equal(ScopeReasons.NotInvolved, decision.Reason);
    }

    [Fact]
    public void Agent_ids_are_matched_case_insensitively()
    {
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId.ToUpperInvariant()), "suite-agent", Changed(CopilotId));

        Assert.True(decision.Included);
    }

    [Fact]
    public void Rule_1_includes_a_cross_cutting_case_no_matter_what_changed()
    {
        // Narrowing exists to skip cases a change cannot affect, and "this change cannot affect
        // safety" is precisely the claim not to accept untested.
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId, crossCutting: true), "suite-agent", Changed(DiagnosisId));

        Assert.True(decision.Included);
        Assert.Equal(ScopeReasons.CrossCutting, decision.Reason);
    }

    [Fact]
    public void Rule_2_includes_everything_for_a_platform_wide_change()
    {
        // A foundation model or provider swap has no agent it demonstrably does not touch, so
        // narrowing switches off rather than being applied against an empty target list.
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId), "suite-agent",
            new ScopeQuery { FullPlatform = true });

        Assert.True(decision.Included);
        Assert.Equal(ScopeReasons.FullPlatform, decision.Reason);
    }

    [Fact]
    public void A_case_with_no_known_agents_is_included_rather_than_dropped()
    {
        // Fail open. An unknown involved set means the harness cannot show the change does not affect
        // this case; excluding it on that basis would produce no result to notice.
        var decision = CaseScope.Decide(Case(), suiteAgentId: null, Changed(CopilotId));

        Assert.True(decision.Included);
        Assert.Equal(ScopeReasons.UnknownAgents, decision.Reason);
    }

    [Fact]
    public void A_disabled_case_is_excluded_and_says_so()
    {
        // The executor skips it, so calling it in scope would overstate coverage by exactly the cases
        // nobody is running. Its own reason, so a disabled cross-cutting safety case stands out
        // instead of blending in with cases the change genuinely cannot affect.
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId, enabled: false, crossCutting: true), "suite-agent",
            Changed(CopilotId));

        Assert.False(decision.Included);
        Assert.Equal(ScopeReasons.Disabled, decision.Reason);
    }

    // ------------------------------------------------------------------ batches

    [Theory]
    [InlineData(CasePriorities.P0, CaseBatches.StopLoss)]
    [InlineData(CasePriorities.P1, CaseBatches.Mandatory)]
    [InlineData(CasePriorities.P2, CaseBatches.Optional)]
    public void Priority_derives_the_batch(string priority, int expected)
    {
        Assert.Equal(expected, CaseBatches.Effective(Case(priority: priority)));
    }

    [Fact]
    public void A_cross_cutting_case_is_batch_one_whatever_its_priority()
    {
        // A safety case that only runs once everything else has passed cannot stop anything, which is
        // the entire job of batch 1.
        Assert.Equal(
            CaseBatches.StopLoss,
            CaseBatches.Effective(Case(priority: CasePriorities.P2, crossCutting: true)));
    }

    [Fact]
    public void An_explicit_batch_overrides_both()
    {
        Assert.Equal(
            CaseBatches.Optional,
            CaseBatches.Effective(Case(priority: CasePriorities.P0, crossCutting: true, batch: CaseBatches.Optional)));
    }

    [Fact]
    public void An_out_of_range_explicit_batch_falls_back_to_the_derivation()
    {
        // Rejected at save time, so this is only reachable for a document written before the check or
        // edited around the API. Falling back beats filing the case in a batch that does not exist,
        // where nothing would ever run it.
        Assert.Equal(CaseBatches.StopLoss, CaseBatches.Effective(Case(priority: CasePriorities.P0, batch: 9)));
    }

    [Fact]
    public void Narrowing_to_a_batch_excludes_the_others_with_their_own_reason()
    {
        // Scheduling, not scoping: batches exist to run in order and stop early, so a case left out
        // of this batch is not out of scope -- it just runs later, and the reason has to say that.
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId, priority: CasePriorities.P2), "suite-agent",
            new ScopeQuery { TargetAgentIds = [CopilotId], Batch = CaseBatches.StopLoss });

        Assert.False(decision.Included);
        Assert.Equal(ScopeReasons.OtherBatch, decision.Reason);
    }

    [Fact]
    public void A_cross_cutting_case_still_belongs_to_batch_one_when_narrowing_by_batch()
    {
        // The two axes have to agree: cross-cutting forces batch 1, so asking for batch 1 has to
        // return it. If they disagreed, the stop-loss batch would run without its safety cases.
        var decision = CaseScope.Decide(
            Case(entryAgentId: DiagnosisId, crossCutting: true, priority: CasePriorities.P2),
            "suite-agent",
            new ScopeQuery { TargetAgentIds = [CopilotId], Batch = CaseBatches.StopLoss });

        Assert.True(decision.Included);
        Assert.Equal(ScopeReasons.CrossCutting, decision.Reason);
        Assert.Equal(CaseBatches.StopLoss, decision.Batch);
    }

    [Fact]
    public void The_decision_reports_the_set_it_was_made_against()
    {
        // A scope nobody can explain is a scope nobody can review, and reviewing it is the only
        // defence against signing off a change against a set that quietly left out the interesting
        // case.
        var decision = CaseScope.Decide(
            Case(entryAgentId: CopilotId, involved: [WoCancellationId]), "suite-agent", Changed(CopilotId));

        Assert.False(decision.Included);
        Assert.Equal([WoCancellationId], decision.InvolvedAgentIds);
    }
}
