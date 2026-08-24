using System.Collections.Generic;
using System.Linq;
using BotSharp.Plugin.AgentTesting.Services;
using BotSharp.Plugin.AgentTesting.Models;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// agentChain is the only assertion that can describe a hand-off, so its three modes are the whole
/// vocabulary available for asserting hand-offs. routedToAgent answers "who spoke last", which
/// cannot express a hand-off at all: for Entry -> A -> B it sees only B, and when control returns to
/// the entry agent and that agent closes the conversation it sees the entry agent, so a correctly
/// routed case reads as a routing failure.
///
/// Two properties matter more than the happy paths and are pinned first: an empty expected list and
/// an unrecognised mode both have to FAIL rather than pass vacuously or fall back to the loosest
/// check, because either would show a case that verified nothing as green.
/// </summary>
public class AgentChainAssertionTests
{
    /// <summary>
    /// Names only, ids left blank. The id-matching path has its own tests at the end of the file,
    /// where the distinction between the two identifiers is the point.
    /// </summary>
    private static AssertionContext Context(params string[] chain) => new()
    {
        AgentChain = chain.Select(name => new AgentChainHop { Id = string.Empty, Name = name }).ToList()
    };

    private static AssertionResult Evaluate(string? expected, string? mode, params string[] chain)
        => AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.AgentChain, Expected = expected, Target = mode },
            Context(chain));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    [InlineData(" , , ")]
    public void An_empty_expected_list_fails_instead_of_passing_vacuously(string? expected)
    {
        // An empty list is a subset of, and an ordered subsequence of, every chain, so Contains and
        // Ordered would both pass while checking nothing. Exact would assert "no agent ever
        // answered", which no author writes on purpose. Note that "," and " , , " parse to an empty
        // list too, so the guard has to be on the parsed result rather than on the raw string.
        var result = Evaluate(expected, mode: null, "Copilot");

        Assert.False(result.Passed);
        Assert.Contains("non-empty", result.Message);
    }

    [Theory]
    [InlineData("orderd")]
    [InlineData("subsequence")]
    [InlineData("any")]
    public void An_unrecognised_mode_fails_rather_than_falling_back_to_the_loosest_check(string mode)
    {
        // Defaulting a typo to Contains would silently downgrade "these agents, in this order" to
        // "these agents, in any order" -- the assertion still goes green, having verified much less
        // than it says. The message names the accepted values so the author can fix it.
        var result = Evaluate("Copilot, Work Order Creator", mode, "Work Order Creator", "Copilot");

        Assert.False(result.Passed);
        Assert.Contains(AgentChainModes.Ordered, result.Message);
        Assert.Contains(mode, result.Message);
    }

    [Fact]
    public void A_blank_mode_means_contains()
    {
        // The common case is "this agent must have been involved", so the default is the loosest
        // mode -- but only when the author left it blank, never as a recovery from a typo.
        var result = Evaluate("Work Order Creator", mode: null, "Copilot", "Work Order Creator");

        Assert.True(result.Passed);
    }

    [Fact]
    public void Contains_ignores_order()
    {
        var result = Evaluate(
            "Work Order Creator, Copilot", AgentChainModes.Contains, "Copilot", "Work Order Creator");

        Assert.True(result.Passed);
    }

    [Fact]
    public void Contains_names_the_agents_that_are_missing()
    {
        // Which agent was absent is the whole diagnostic. "the chain does not match" would leave the
        // author diffing two lists by eye.
        var result = Evaluate(
            "Copilot, Diagnosis, Work Order Creator", AgentChainModes.Contains, "Copilot", "Work Order Creator");

        Assert.False(result.Passed);
        Assert.Contains("Diagnosis", result.Message);
        Assert.DoesNotContain("Copilot", result.Message);
    }

    [Fact]
    public void Ordered_allows_other_agents_in_between()
    {
        // Asserting the hand-offs an author cares about must not require enumerating every agent the
        // conversation happened to pass through -- that is what Exact is for.
        var result = Evaluate(
            "Copilot, Work Order Creator", AgentChainModes.Ordered,
            "Copilot", "Diagnosis", "Work Order Creator");

        Assert.True(result.Passed);
    }

    [Fact]
    public void Ordered_rejects_the_reverse_order()
    {
        // The one thing Contains cannot see. Both agents are present, so Contains would pass; the
        // hand-off went the wrong way round.
        var chain = new[] { "Work Order Creator", "Copilot" };

        Assert.True(Evaluate("Copilot, Work Order Creator", AgentChainModes.Contains, chain).Passed);
        Assert.False(Evaluate("Copilot, Work Order Creator", AgentChainModes.Ordered, chain).Passed);
    }

    [Fact]
    public void Ordered_matches_a_repeated_agent_against_a_return_hop()
    {
        // The runner collapses only CONSECUTIVE repeats, so a genuine return hop survives in the
        // chain -- and this is the assertion that reads it. Requiring Copilot twice must match
        // Copilot -> WO -> Copilot and not Copilot -> WO.
        Assert.True(Evaluate(
            "Copilot, Work Order Creator, Copilot", AgentChainModes.Ordered,
            "Copilot", "Work Order Creator", "Copilot").Passed);

        Assert.False(Evaluate(
            "Copilot, Work Order Creator, Copilot", AgentChainModes.Ordered,
            "Copilot", "Work Order Creator").Passed);
    }

    [Fact]
    public void Exact_rejects_an_extra_agent_that_ordered_would_allow()
    {
        var chain = new[] { "Copilot", "Diagnosis", "Work Order Creator" };

        Assert.True(Evaluate("Copilot, Work Order Creator", AgentChainModes.Ordered, chain).Passed);
        Assert.False(Evaluate("Copilot, Work Order Creator", AgentChainModes.Exact, chain).Passed);
    }

    [Fact]
    public void Exact_on_a_single_agent_is_how_isolation_is_asserted()
    {
        // An Agent case is supposed to measure one agent with the router out of the picture, but
        // route_to_agent stays on the allow list during it, so a leaf agent that routes onward turns
        // the case into a multi-agent one with nothing to show it. An Exact chain of one agent is the
        // assertion that catches that.
        Assert.True(Evaluate("Work Order Creator", AgentChainModes.Exact, "Work Order Creator").Passed);
        Assert.False(Evaluate(
            "Work Order Creator", AgentChainModes.Exact, "Work Order Creator", "Diagnosis").Passed);
    }

    [Fact]
    public void Agent_names_are_matched_case_insensitively_and_trimmed()
    {
        // Authors type agent names by hand and copy them out of the UI, so casing and stray spaces
        // around the commas are not a reason to fail a case. Mirrors routedToAgent, which has always
        // compared OrdinalIgnoreCase.
        var result = Evaluate(
            "  copilot ,   WORK ORDER CREATOR  ", AgentChainModes.Ordered, "Copilot", "Work Order Creator");

        Assert.True(result.Passed);
    }

    [Fact]
    public void The_actual_chain_is_reported_in_a_readable_form()
    {
        // The chain is what the author has to reason about when the assertion fails, and a bare
        // list would be rendered by the UI as an opaque blob. Arrow-joined mirrors how the hand-off
        // reads in the conversation.
        var result = Evaluate("Nobody", AgentChainModes.Contains, "Copilot", "Work Order Creator");

        Assert.Equal("Copilot -> Work Order Creator", result.Actual);
    }

    [Fact]
    public void An_empty_chain_fails_every_mode_but_never_throws()
    {
        // A case whose agent never answered (or one that errored before any turn ran) leaves the
        // chain empty. That has to be an ordinary failing assertion, not an exception that turns the
        // case into an infrastructure Error and hides the real problem.
        foreach (var mode in AgentChainModes.All)
        {
            var result = Evaluate("Copilot", mode);

            Assert.False(result.Passed);
            Assert.Equal(string.Empty, result.Actual);
        }
    }

    [Fact]
    public void Save_time_validation_requires_the_expected_list()
    {
        // The same guard as at evaluation time, but at case create/update, so a chain assertion with
        // nothing to compare against is rejected while its author is still looking at it.
        Assert.NotNull(AssertionValidation.Validate(
            new TestAssertion { Type = AssertionTypes.AgentChain, Target = AgentChainModes.Ordered }));

        Assert.Null(AssertionValidation.Validate(
            new TestAssertion { Type = AssertionTypes.AgentChain, Expected = "Copilot" }));
    }

    private static AssertionContext ChainOf(params string[] idAndName) => new()
    {
        // Pairs, flattened: ("id1", "Name 1", "id2", "Name 2").
        AgentChain = Enumerable.Range(0, idAndName.Length / 2)
            .Select(i => new AgentChainHop { Id = idAndName[i * 2], Name = idAndName[i * 2 + 1] })
            .ToList()
    };

    [Fact]
    public void An_expected_agent_may_be_given_as_an_id_instead_of_a_name()
    {
        // The id is exactly what an author copies out of the agent list, and the first real routing
        // case did precisely that: it asserted routedToAgent against a guid while the chain reported
        // a display name, so it could never pass no matter what the agent did.
        var context = ChainOf("0fe3905d-75f1-4e2e-8e54-ec3d33d6b6f0", "WO Cancellation");

        var byId = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.RoutedToAgent,
                Expected = "0fe3905d-75f1-4e2e-8e54-ec3d33d6b6f0"
            },
            context);

        var byName = AssertionEvaluator.Evaluate(
            new TestAssertion { Type = AssertionTypes.RoutedToAgent, Expected = "WO Cancellation" },
            context);

        Assert.True(byId.Passed);
        Assert.True(byName.Passed);

        // Either way the reported actual is the readable name, never the guid.
        Assert.Equal("WO Cancellation", byId.Actual);
    }

    [Fact]
    public void An_agent_chain_accepts_ids_names_and_a_mixture_of_both()
    {
        // Authors paste whichever identifier is in front of them, and a chain listing several agents
        // is the most likely place to end up with a mixture.
        var context = ChainOf(
            "2cd4b805-7078-4405-87e9-2ec9aadf8a11", "Lessen Copilot",
            "0fe3905d-75f1-4e2e-8e54-ec3d33d6b6f0", "WO Cancellation");

        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.AgentChain,
                Target = AgentChainModes.Ordered,
                Expected = "2cd4b805-7078-4405-87e9-2ec9aadf8a11, WO Cancellation"
            },
            context);

        Assert.True(result.Passed);
    }

    [Fact]
    public void A_wrong_id_still_fails()
    {
        // Accepting either identifier must not become accepting anything: an id belonging to some
        // other agent has to fail exactly as a wrong name does.
        var result = AssertionEvaluator.Evaluate(
            new TestAssertion
            {
                Type = AssertionTypes.RoutedToAgent,
                Expected = "11111111-2222-3333-4444-555555555555"
            },
            ChainOf("0fe3905d-75f1-4e2e-8e54-ec3d33d6b6f0", "WO Cancellation"));

        Assert.False(result.Passed);
    }
}
