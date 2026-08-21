namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Raised when the judge could not reach a verdict at all: no judge model is configured on the
/// suite, its provider is not registered, the vendor call failed, or the reply could not be read as
/// a score.
///
/// Deliberately an exception rather than a failing <see cref="AssertionResult"/>. "The judge could
/// not decide" is not "the agent regressed" -- folding the two together would let a vendor timeout,
/// a rate limit or a malformed reply read as an agent defect, which is exactly the Failed/Error
/// confusion the runner keeps apart everywhere else. <see cref="AgentTestCaseRunner"/> turns this
/// into a case-level Error carrying this message.
/// </summary>
public class AgentTestJudgeUnavailableException : Exception
{
    public AgentTestJudgeUnavailableException(string message) : base(message)
    {
    }

    public AgentTestJudgeUnavailableException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Scores one llmJudge assertion with a model.
///
/// This is the one assertion type that is NOT a pure function, which is why it lives here instead of
/// in <see cref="AssertionEvaluator"/>: that class is a pure, synchronous, I/O-free function, and the
/// runner depends on that purity for reproducible verdicts. A model call is neither pure nor
/// synchronous, so llmJudge is evaluated in a separate pass and the reproducible assertions stay
/// reproducible.
///
/// What reaches the vendor is deliberately narrow: the fixed rubric, the criterion the case author
/// wrote (<see cref="TestAssertion.Expected"/>) and the agent's reply text. Tool arguments, tool
/// results, conversation state and the user's own messages are NOT sent. That is the same boundary
/// <see cref="ICaseSegmenter"/> draws, with one unavoidable difference: judging the quality of a
/// reply requires sending that reply, and an agent's reply can contain PII (phone numbers,
/// addresses, tenant names). Callers need to know that. It also means a criterion has to be
/// self-contained -- "the reply asks for a work order number" works, "the reply answers the user's
/// question" does not, because the judge never sees the question.
/// </summary>
public interface IAgentTestJudge
{
    /// <summary>
    /// Scores <paramref name="assertion"/> against <paramref name="context"/>. Throws
    /// <see cref="AgentTestJudgeUnavailableException"/> when no verdict could be reached; a returned
    /// result is always a real verdict, passing or failing.
    /// </summary>
    Task<AssertionResult> JudgeAsync(
        TestAssertion assertion,
        AssertionContext context,
        AgentTestSuite suite,
        CancellationToken ct);
}
