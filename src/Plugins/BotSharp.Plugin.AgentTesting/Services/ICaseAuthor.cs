namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// Authors and edits a test case by conversation: the user picks an agent's suite, says what they
/// want in their own words, and gets back a draft they can keep talking to.
///
/// Contract:
/// - Produces a draft, never a stored case. Nothing here writes to the case store; the response goes
///   back to the editor, and the human presses save. See <see cref="Models.AgentTestAuthorResponse"/>.
/// - Never deletes by omission. The model declares which fields it is changing and only those are
///   taken from its answer; everything else is copied off the draft that came in. See
///   <see cref="Models.AuthorFields"/>.
/// - Never invents a callable function. A mock or a toolCalled assertion naming a function the agent
///   cannot call is dropped and reported, because it could otherwise never match at run time.
/// - Runs <see cref="CaseValidation"/> on its own output and gives the model one chance to repair a
///   rejection against the real error text. A draft that still fails is returned as the unchanged
///   original plus the errors -- never as a broken draft presented as progress.
///
/// Boundary -- what leaves this system. The agent's own instruction, its function names and
/// descriptions, the suite's existing case names, the draft, and the user's authoring messages are
/// all sent to the configured model vendor. When the case has run before, so are the agent's actual
/// reply texts and its actual tool arguments from that run, which for a case recorded from a real
/// conversation can include the customer data that conversation contained. That is a wider egress
/// than the recorder's (which withholds tool arguments and results), and it is why the endpoint is
/// admin-only.
/// </summary>
public interface ICaseAuthor
{
    /// <summary>
    /// One authoring turn.
    /// </summary>
    /// <exception cref="CaseAuthorUnavailableException">
    /// No usable model, the vendor call failed, or the model's answer could not be read as an
    /// authoring result. All of these mean "no draft was produced", which is a different thing from
    /// "the draft is invalid" -- the latter comes back in the response.
    /// </exception>
    Task<AgentTestAuthorResponse> AuthorAsync(
        AgentTestSuite suite,
        AgentTestAuthorRequest request,
        CancellationToken ct);
}

/// <summary>
/// No draft could be produced. Separate from a validation failure on purpose: a validation failure
/// still returns a draft and a next step, while this means the authoring turn did not happen and the
/// user should retry or fix configuration.
/// </summary>
public class CaseAuthorUnavailableException : Exception
{
    public CaseAuthorUnavailableException(string message) : base(message) { }
    public CaseAuthorUnavailableException(string message, Exception inner) : base(message, inner) { }
}
