using System.Text;
using System.Text.Json;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Plugin.AgentTesting.Services;

/// <summary>
/// The model-backed <see cref="ICaseSegmenter"/>. Contract and boundary live on the interface --
/// this class only asks a model where to cut, and extends its answer no trust: out-of-range,
/// overlapping, out-of-order or unnamed segments are all rejected. Better to fail and let a human
/// retry than to accept a result that looks usable but cut the cases in the wrong places.
/// </summary>
public class LlmCaseSegmenter : ICaseSegmenter
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LlmCaseSegmenter> _logger;

    public LlmCaseSegmenter(IServiceProvider services, ILogger<LlmCaseSegmenter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CaseSegment>> SegmentAsync(
        IReadOnlyList<SegmentableTurn> turns,
        TestModel model,
        CancellationToken ct)
    {
        if (turns.Count == 0)
        {
            return [];
        }

        // Nothing to decide with a single turn, and it saves a model call on the commonest case.
        if (turns.Count == 1)
        {
            return [new CaseSegment(FallbackName(turns[0]), 0, 0)];
        }

        ct.ThrowIfCancellationRequested();

        // Resolved straight from DI rather than through BotSharp.Core's CompletionProvider helper,
        // for two reasons: this plugin only references BotSharp.Abstraction (adding a BotSharp.Core
        // project reference for one call would change its build topology, which differs between
        // DebugBrain.sln and OneBrain.sln in the consuming host), and that helper also writes provider/model into the
        // ambient conversation state -- unwanted here, since segmentation is a one-off call that
        // belongs to no conversation.
        var completion = _services.GetServices<IChatCompletion>()
            .FirstOrDefault(x => string.Equals(x.Provider, model.Provider, StringComparison.OrdinalIgnoreCase));

        if (completion == null)
        {
            throw new InvalidOperationException($"no chat completion provider is registered for '{model.Provider}'");
        }

        completion.SetModelName(model.Model);

        var promptAgent = new Agent
        {
            Id = Guid.Empty.ToString(),
            Name = "AgentTestCaseSegmenter",
            Instruction = BuildInstruction()
        };

        var response = await completion.GetChatCompletions(
            promptAgent,
            [new RoleDialogModel(AgentRole.User, BuildTranscript(turns))]);

        var raw = response?.Content ?? string.Empty;
        var segments = Parse(raw, turns.Count);

        _logger.LogInformation(
            "Case segmenter split a {TurnCount}-turn conversation into {SegmentCount} case(s) using {Model}.",
            turns.Count, segments.Count, model);

        return segments;
    }

    private static string FallbackName(SegmentableTurn turn)
    {
        var text = turn.UserMessage.Trim();
        return string.IsNullOrEmpty(text) ? "Recorded case" : Truncate(text, 60);
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max].TrimEnd() + "...";

    private static string BuildInstruction() =>
        """
        You split a recorded support conversation into independent regression test cases.

        A case is one self-contained thing the user wanted. Start a new case when the user moves on
        to a different goal (for example: from "where is my technician" to "reschedule my
        appointment"). Follow-up turns that refine, confirm or correct the SAME goal belong to the
        same case.

        Rules:
        - Cover every turn exactly once. Segments must be contiguous, in order, non-overlapping,
          and together span turn 0 through the last turn.
        - Prefer few, meaningful cases. If the whole conversation is one goal, return one segment.
        - `name` is a short human label for the case, at most 60 characters, in the language the
          user wrote in. Describe the user's goal, not the tools that were called.

        Reply with JSON only, no prose and no code fence:
        {"segments":[{"name":"...","firstTurn":0,"lastTurn":2}]}
        """;

    private static string BuildTranscript(IReadOnlyList<SegmentableTurn> turns)
    {
        var builder = new StringBuilder();
        foreach (var turn in turns)
        {
            builder.Append("Turn ").Append(turn.Index).Append(": ").AppendLine(turn.UserMessage);
            if (turn.ToolNames.Count > 0)
            {
                // Names only. Arguments and results stay in this process -- see ICaseSegmenter.
                builder.Append("  tools called: ").AppendLine(string.Join(", ", turn.ToolNames));
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Parses and validates the model's output. Any single violation rejects the whole thing: a
    /// half-correct segmentation cuts cases in the wrong place, and that kind of error looks
    /// entirely normal in the UI (plausible names, real turns) until somebody actually runs it.
    /// </summary>
    public static IReadOnlyList<CaseSegment> Parse(string raw, int turnCount)
    {
        var json = ExtractJson(raw);
        if (json == null)
        {
            throw new InvalidOperationException(
                $"the segmenter model did not return JSON. First 200 chars: {Truncate(raw, 200)}");
        }

        SegmentEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<SegmentEnvelope>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"the segmenter model returned malformed JSON: {ex.Message}");
        }

        var parsed = envelope?.Segments;
        if (parsed is not { Count: > 0 })
        {
            throw new InvalidOperationException("the segmenter model returned no segments");
        }

        var expectedNext = 0;
        var result = new List<CaseSegment>();
        foreach (var segment in parsed)
        {
            if (segment.FirstTurn != expectedNext)
            {
                throw new InvalidOperationException(
                    $"the segmenter model returned a gap or overlap: expected the next segment to start at "
                    + $"turn {expectedNext}, got {segment.FirstTurn}");
            }

            if (segment.LastTurn < segment.FirstTurn || segment.LastTurn >= turnCount)
            {
                throw new InvalidOperationException(
                    $"the segmenter model returned an out-of-range segment {segment.FirstTurn}..{segment.LastTurn} "
                    + $"for a conversation with {turnCount} turn(s)");
            }

            var name = string.IsNullOrWhiteSpace(segment.Name)
                ? $"Turns {segment.FirstTurn}-{segment.LastTurn}"
                : Truncate(segment.Name.Trim(), 60);

            result.Add(new CaseSegment(name, segment.FirstTurn, segment.LastTurn));
            expectedNext = segment.LastTurn + 1;
        }

        if (expectedNext != turnCount)
        {
            throw new InvalidOperationException(
                $"the segmenter model left turn(s) {expectedNext}..{turnCount - 1} uncovered");
        }

        return result;
    }

    /// <summary>
    /// Tolerates a model wrapping its JSON in a ```json fence or a sentence of prose -- the most
    /// common and most harmless way to disobey "JSON only", and not worth failing over. First '{'
    /// through last '}'.
    /// </summary>
    private static string? ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private sealed class SegmentEnvelope
    {
        public List<SegmentDto>? Segments { get; set; }
    }

    private sealed class SegmentDto
    {
        public string? Name { get; set; }
        public int FirstTurn { get; set; }
        public int LastTurn { get; set; }
    }
}
