namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// One scenario inside a recorded conversation: turns <see cref="FirstTurn"/> through
/// <see cref="LastTurn"/> inclusive (0-based, counted by user message), to become its own case.
/// </summary>
public record CaseSegment(string Name, int FirstTurn, int LastTurn);

/// <summary>One turn as the segmenter sees it -- deliberately carries no function arguments and no
/// return content, see <see cref="ICaseSegmenter"/>.</summary>
public class SegmentableTurn
{
    public int Index { get; set; }
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Names (only the names) of the functions this turn called.</summary>
    public IReadOnlyList<string> ToolNames { get; set; } = [];
}

/// <summary>
/// Splits a conversation into one or more scenarios. A segmenter decides ONLY where to cut and what
/// to call each piece -- mock return values, assertions and state are still generated verbatim from
/// the real conversation by <see cref="AgentTestRecorder.BuildDraft"/>, which no model touches.
///
/// That boundary is deliberate, not laziness:
/// 1) the moment a mock's ResultContent is written by a model, the case stops being a replay of a
///    call that really happened -- and replay is the entire reason recording exists;
/// 2) letting a model write assertions inevitably produces output-text assertions like
///    outputContains, which go red on any rewording -- exactly what the AgentTestRecorder class
///    comment already rules out.
///
/// An implementation only ever sees the user messages and which function NAMES each turn called --
/// function arguments and return content do not leave this process. The densest PII in a
/// conversation (addresses, phone numbers, work order details) usually sits in those arguments and
/// results, and segmentation does not need them. User messages can still contain PII; that cannot be
/// removed, and callers need to know it.
/// </summary>
public interface ICaseSegmenter
{
    Task<IReadOnlyList<CaseSegment>> SegmentAsync(
        IReadOnlyList<SegmentableTurn> turns,
        TestModel model,
        CancellationToken ct);
}
