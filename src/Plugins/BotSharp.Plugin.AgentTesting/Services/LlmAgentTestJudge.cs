using System.Text.Json;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The model-backed <see cref="IAgentTestJudge"/>. Contract, boundary and the reason this is not part
/// of <see cref="AssertionEvaluator"/> all live on the interface.
///
/// Extends the model's answer no trust: a blank reply, a non-JSON reply, malformed JSON or a score
/// outside the 1-5 scale are all rejected as "no verdict" rather than coerced into a pass or a fail.
/// A judge that ignored the rubric has not graded anything, and reading pass/fail out of that is
/// reading meaning into noise.
/// </summary>
public class LlmAgentTestJudge : IAgentTestJudge
{
    /// <summary>
    /// Pass mark when a case does not set <see cref="TestAssertion.MinScore"/>. 4/5 is the bar the
    /// evaluation framework uses for its own human quality score, and the rubric below is worded to
    /// match that framework's 1-5 definitions -- so a model score and a human score mean the same
    /// thing on the same scale, and the two can be compared or swapped later.
    /// </summary>
    public const double DefaultMinScore = 4;

    private const double MinValidScore = 1;
    private const double MaxValidScore = 5;

    private readonly IServiceProvider _services;
    private readonly ILogger<LlmAgentTestJudge> _logger;

    public LlmAgentTestJudge(IServiceProvider services, ILogger<LlmAgentTestJudge> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<AssertionResult> JudgeAsync(
        TestAssertion assertion,
        AssertionContext context,
        AgentTestSuite suite,
        CancellationToken ct)
    {
        var criterion = assertion.Expected;
        if (string.IsNullOrWhiteSpace(criterion))
        {
            // AssertionValidation already rejects this at case create/update. Repeated here because
            // a case saved before that rule existed would otherwise reach the vendor with an empty
            // criterion and come back with a meaningless score.
            throw new AgentTestJudgeUnavailableException(
                "this llmJudge assertion has no 'expected' criterion to judge against");
        }

        // No silent default. BotSharp's own InstructService falls back to openai/gpt-4o when a
        // provider and model are not given; inheriting that here would score cases with a model
        // nobody chose, and the run would look conclusive. A missing judge model is an Error with a
        // message naming the fix.
        if (string.IsNullOrWhiteSpace(suite.JudgeProvider) || string.IsNullOrWhiteSpace(suite.JudgeModel))
        {
            throw new AgentTestJudgeUnavailableException(
                "this suite has no judge model configured, so llmJudge assertions cannot be scored. "
                + "Set the suite's judgeProvider and judgeModel, or remove the llmJudge assertion.");
        }

        if (string.IsNullOrWhiteSpace(context.Output))
        {
            // Not a failing verdict: the agent produced no text at all, so there is nothing for the
            // judge to grade. Whatever went wrong upstream is the real finding, and the other
            // assertions on this case will say so far more usefully than a fabricated score would.
            throw new AgentTestJudgeUnavailableException(
                "the agent produced no reply text, so there is nothing for llmJudge to score");
        }

        ct.ThrowIfCancellationRequested();

        // Resolved straight from DI rather than through BotSharp.Core's CompletionProvider helper,
        // for the same two reasons LlmCaseSegmenter gives: this plugin references only
        // BotSharp.Abstraction, and that helper also writes provider/model into the ambient
        // conversation state -- which here would leak the JUDGE's model into the conversation under
        // test and change what the agent itself runs on.
        var completion = _services.GetServices<IChatCompletion>()
            .FirstOrDefault(x => string.Equals(x.Provider, suite.JudgeProvider, StringComparison.OrdinalIgnoreCase));

        if (completion == null)
        {
            throw new AgentTestJudgeUnavailableException(
                $"no chat completion provider is registered for judge provider '{suite.JudgeProvider}'");
        }

        completion.SetModelName(suite.JudgeModel);

        var promptAgent = new Agent
        {
            Id = Guid.Empty.ToString(),
            Name = "AgentTestJudge",
            Instruction = BuildInstruction()
        };

        string raw;
        try
        {
            var response = await completion.GetChatCompletions(
                promptAgent,
                [new RoleDialogModel(AgentRole.User, BuildPrompt(criterion, context.Output))]);

            raw = response?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            // A vendor timeout, a rate limit, a bad key. None of these say anything about the agent
            // under test, so none of them may surface as a failing assertion.
            throw new AgentTestJudgeUnavailableException(
                $"the judge model call failed: {ex.Message}", ex);
        }

        var verdict = ParseVerdict(raw);
        var threshold = assertion.MinScore ?? DefaultMinScore;

        var result = new AssertionResult
        {
            Type = assertion.Type,
            Target = assertion.Target,
            Expected = criterion,
            Actual = verdict.Score.ToString("0.#"),
            Score = verdict.Score,
            Passed = verdict.Score >= threshold
        };

        // The reason is recorded whether it passed or failed: a pass at exactly the threshold is
        // worth being able to read afterwards, and a judge's stated reason is the only way to tell
        // "graded correctly" from "graded plausibly but wrongly".
        result.Message = string.IsNullOrWhiteSpace(verdict.Reason)
            ? $"judged {verdict.Score:0.#}/5 against a threshold of {threshold:0.#}"
            : $"judged {verdict.Score:0.#}/5 against a threshold of {threshold:0.#}: {verdict.Reason}";

        _logger.LogInformation(
            "llmJudge scored {Score}/5 (threshold {Threshold}) using {Provider}/{Model}.",
            verdict.Score, threshold, suite.JudgeProvider, suite.JudgeModel);

        return result;
    }

    /// <summary>
    /// Parses and validates the judge's reply. Public and static so it can be unit-tested without a
    /// vendor: everything that can realistically go wrong with a model's answer is decided here.
    /// </summary>
    public static JudgeVerdict ParseVerdict(string raw)
    {
        var json = ExtractJson(raw);
        if (json == null)
        {
            throw new AgentTestJudgeUnavailableException(
                $"the judge model did not return JSON. First 200 chars: {Truncate(raw, 200)}");
        }

        JudgeVerdict? verdict;
        try
        {
            verdict = JsonSerializer.Deserialize<JudgeVerdict>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new AgentTestJudgeUnavailableException(
                $"the judge model returned malformed JSON: {ex.Message}");
        }

        if (verdict == null)
        {
            throw new AgentTestJudgeUnavailableException("the judge model returned an empty verdict");
        }

        if (verdict.Score < MinValidScore || verdict.Score > MaxValidScore || double.IsNaN(verdict.Score))
        {
            // A score off the scale means the rubric was not followed. Clamping it would silently
            // turn "did not grade" into a grade.
            throw new AgentTestJudgeUnavailableException(
                $"the judge model returned {verdict.Score:0.#}, outside the 1-5 scale it was given");
        }

        return verdict;
    }

    private static string? ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max].TrimEnd() + "...";

    /// <summary>
    /// The 1-5 definitions are worded to match the evaluation framework's human scoring scale, so
    /// that a model score and a human score are directly comparable. Changing them decouples the two.
    /// </summary>
    private static string BuildInstruction() =>
        """
        You grade one reply produced by a customer-service AI agent, as part of an automated
        regression test.

        You are given exactly one CRITERION and the agent's REPLY. Judge only how well the reply
        satisfies that criterion. Do not grade tone, length, formatting or style unless the criterion
        asks about them. Do not judge whether the underlying business facts are true -- you have no
        way to check them and other assertions already cover that.

        Score on this scale:
        5 - clearly satisfies the criterion, no substantive problem
        4 - satisfies the criterion, only minor problems
        3 - broadly usable, but with an obvious gap
        2 - noticeable problems that affect use
        1 - unacceptable

        Reply with JSON only. No prose, no code fence, no explanation outside the JSON:
        {"score": <integer 1-5>, "reason": "<one sentence, under 200 characters>"}
        """;

    private static string BuildPrompt(string criterion, string output) =>
        $"""
        CRITERION:
        {criterion}

        REPLY:
        {output}
        """;
}

/// <summary>One judge verdict, as returned by the model.</summary>
public class JudgeVerdict
{
    public double Score { get; set; }
    public string? Reason { get; set; }
}
