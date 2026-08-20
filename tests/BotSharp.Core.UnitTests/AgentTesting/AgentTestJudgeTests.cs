using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BotSharp.Plugin.AgentTesting.Models;
using BotSharp.Plugin.AgentTesting.Runtime;
using BotSharp.Plugin.AgentTesting.Services;
using Xunit;

namespace BotSharp.Core.UnitTests.AgentTesting;

/// <summary>
/// llmJudge is the one assertion type that is not a pure function, and that makes two things worth
/// pinning here.
///
/// First, the model's answer is extended no trust: a blank reply, a non-JSON reply, malformed JSON or
/// a score off the 1-5 scale are all "no verdict", never a coerced pass or fail. A judge that ignored
/// the rubric has graded nothing, and reading pass/fail out of that is reading meaning into noise.
///
/// Second, and more important, every way the judge can fail to reach a verdict has to land on Error,
/// not Failed. A vendor timeout, an unconfigured judge model or a rate limit says nothing about the
/// agent under test -- if any of them surfaced as a failing assertion, provider noise would be
/// indistinguishable from an agent regression, which is the whole reason the harness keeps Failed and
/// Error apart.
/// </summary>
public class AgentTestJudgeTests
{
    // ---- ParseVerdict: extend the model's answer no trust ---------------------------------------

    [Fact]
    public void Parses_a_plain_json_verdict()
    {
        var verdict = LlmAgentTestJudge.ParseVerdict("""{"score": 4, "reason": "asks for the work order number"}""");

        Assert.Equal(4, verdict.Score);
        Assert.Equal("asks for the work order number", verdict.Reason);
    }

    [Fact]
    public void Parses_a_verdict_wrapped_in_a_code_fence_or_prose()
    {
        // Models add fences and preambles despite being told not to. Rejecting those would make the
        // feature fail for a formatting habit rather than for a real problem.
        var verdict = LlmAgentTestJudge.ParseVerdict(
            "Sure, here is my assessment:\n```json\n{\"score\": 5, \"reason\": \"clear\"}\n```\n");

        Assert.Equal(5, verdict.Score);
        Assert.Equal("clear", verdict.Reason);
    }

    [Fact]
    public void Accepts_property_names_in_any_case()
    {
        var verdict = LlmAgentTestJudge.ParseVerdict("""{"Score": 3, "Reason": "partial"}""");

        Assert.Equal(3, verdict.Score);
        Assert.Equal("partial", verdict.Reason);
    }

    [Fact]
    public void Accepts_a_verdict_with_no_reason()
    {
        // The reason is for a human reading the result afterwards; its absence must not invalidate a
        // score the model did give.
        var verdict = LlmAgentTestJudge.ParseVerdict("""{"score": 5}""");

        Assert.Equal(5, verdict.Score);
        Assert.Null(verdict.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("I would rate this a 4 out of 5.")]                 // no JSON at all
    [InlineData("{\"score\": }")]                                   // malformed
    [InlineData("{\"reason\": \"good\"}")]                          // no score -> defaults to 0, off scale
    public void Rejects_a_reply_it_cannot_read_as_a_score(string raw)
    {
        Assert.Throws<AgentTestJudgeUnavailableException>(() => LlmAgentTestJudge.ParseVerdict(raw));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Rejects_a_score_outside_the_scale_instead_of_clamping_it(double score)
    {
        // Clamping would silently turn "did not follow the rubric" into a grade. 6 clamped to 5
        // would even pass, which is the worst of the available outcomes.
        var ex = Assert.Throws<AgentTestJudgeUnavailableException>(
            () => LlmAgentTestJudge.ParseVerdict($"{{\"score\": {score}}}"));

        Assert.Contains("1-5", ex.Message);
    }

    // ---- JudgeAsync guards: everything decidable without a vendor -------------------------------

    [Fact]
    public async Task Refuses_to_judge_when_the_suite_has_no_judge_model()
    {
        // The reason this check exists at all: BotSharp's InstructService silently falls back to
        // openai/gpt-4o when no provider and model are given. Inheriting that would score cases with
        // a model nobody chose, and the run would still look conclusive.
        var ex = await Assert.ThrowsAsync<AgentTestJudgeUnavailableException>(
            () => Judge().JudgeAsync(Assertion(), Context("we need your work order number"), new AgentTestSuite
            {
                Id = "suite-1",
                AgentId = "agent-1",
                Name = "s"
            }, CancellationToken.None));

        // The message has to name the fix -- whoever sees this in the UI is the person who has to
        // configure the suite.
        Assert.Contains("judgeProvider", ex.Message);
        Assert.Contains("judgeModel", ex.Message);
    }

    [Fact]
    public async Task Refuses_to_judge_an_assertion_with_no_criterion()
    {
        // AssertionValidation already rejects this at save time. Repeated in the judge because a case
        // stored before that rule existed would otherwise reach the vendor with an empty criterion
        // and come back with a meaningless score.
        await Assert.ThrowsAsync<AgentTestJudgeUnavailableException>(
            () => Judge().JudgeAsync(
                new TestAssertion { Type = AssertionTypes.LlmJudge, Expected = "   " },
                Context("anything"),
                ConfiguredSuite(),
                CancellationToken.None));
    }

    [Fact]
    public async Task Refuses_to_judge_when_the_agent_produced_no_reply()
    {
        // Not a failing verdict: there is nothing to grade. Whatever went wrong upstream is the real
        // finding, and this case's other assertions report it far more usefully than a fabricated
        // score would.
        await Assert.ThrowsAsync<AgentTestJudgeUnavailableException>(
            () => Judge().JudgeAsync(Assertion(), Context(null), ConfiguredSuite(), CancellationToken.None));
    }

    [Fact]
    public async Task Reports_an_unregistered_judge_provider_as_no_verdict()
    {
        var ex = await Assert.ThrowsAsync<AgentTestJudgeUnavailableException>(
            () => Judge().JudgeAsync(Assertion(), Context("some reply"), ConfiguredSuite(provider: "not-installed"),
                CancellationToken.None));

        Assert.Contains("not-installed", ex.Message);
    }

    // ---- The Failed/Error split, end to end through the runner ----------------------------------

    [Fact]
    public async Task An_unjudgeable_case_is_Error_not_Failed()
    {
        // The single most important behaviour in this file. If this ever reports Failed, every
        // vendor hiccup starts reading as an agent regression, and a run's Failed count stops
        // meaning anything.
        var driver = new StubDriver("we need your work order number");
        var runner = new AgentTestCaseRunner(
            new AgentTestRunRegistry(),
            driver,
            NullLogger<AgentTestCaseRunner>.Instance,
            judge: null);       // no judge registered at all

        var result = await runner.RunAsync(ConfiguredSuite(), new AgentTestCase
        {
            Id = "case-1",
            SuiteId = "suite-1",
            Name = "c",
            Turns = [new TestTurn { Index = 0, UserMessage = "where is my work order" }],
            Assertions = [Assertion()]
        }, "run-1", null, CancellationToken.None);

        Assert.Equal(AgentTestStatus.Error, result.Status);
        Assert.Contains("llmJudge", result.Error);
    }

    // ---- helpers -------------------------------------------------------------------------------

    private static LlmAgentTestJudge Judge()
        => new(new ServiceCollection().BuildServiceProvider(), NullLogger<LlmAgentTestJudge>.Instance);

    private static TestAssertion Assertion() => new()
    {
        Type = AssertionTypes.LlmJudge,
        Expected = "the reply asks the user for a work order number"
    };

    private static AssertionContext Context(string? output) => new() { Output = output };

    private static AgentTestSuite ConfiguredSuite(string provider = "openai") => new()
    {
        Id = "suite-1",
        AgentId = "agent-1",
        Name = "s",
        CaseTimeoutSeconds = 120,
        JudgeProvider = provider,
        JudgeModel = "gpt-4o"
    };

    /// <summary>Minimal driver: a live seam and one canned reply, so the runner reaches the judge.</summary>
    private sealed class StubDriver : IAgentConversationDriver
    {
        private readonly string _reply;

        public StubDriver(string reply) => _reply = reply;

        public Task PrepareAsync(string conversationId, string agentId, IReadOnlyList<TestState> initialStates)
            => Task.CompletedTask;

        public Task<string?> SendAsync(string conversationId, string agentId, string userMessage, CancellationToken ct)
            => Task.FromResult<string?>(_reply);

        public Task<bool> RunCanaryAsync(string conversationId, string agentId, CancellationToken ct)
            => Task.FromResult(true);

        public Task<IReadOnlyDictionary<string, string?>> ReadStatesAsync(string conversationId)
            => Task.FromResult<IReadOnlyDictionary<string, string?>>(new Dictionary<string, string?>());

        public Task<IReadOnlyList<AgentChainHop>> ReadAssistantAgentSequenceAsync(string conversationId)
            => Task.FromResult<IReadOnlyList<AgentChainHop>>([]);

        public Task<int> InjectHistoryAsync(
            string conversationId, string agentId, IReadOnlyList<TestHistoryMessage> history)
            => Task.FromResult(history.Count);
    }
}
