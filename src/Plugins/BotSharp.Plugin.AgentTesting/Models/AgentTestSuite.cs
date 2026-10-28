namespace BotSharp.Plugin.AgentTesting.Models;

public class AgentTestSuite : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Model used by llmJudge. When unconfigured, llmJudge assertions fail outright rather than
    /// passing silently.
    /// </summary>
    public string? JudgeProvider { get; set; }
    public string? JudgeModel { get; set; }

    /// <summary>Functions let through on top of the default control-flow allow list.</summary>
    public List<string> ExtraAllowedFunctions { get; set; } = [];

    /// <summary>Functions blocked outright; wins over the allow list.</summary>
    public List<string> ForceBlockedFunctions { get; set; } = [];

    public int CaseTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// How many cases of this suite execute at the same time. One run's cases are independent by
    /// construction -- each one opens its own conversation and gets its own DI scope, and the mock
    /// seam is keyed by conversation id -- so the only thing serial execution ever bought was a
    /// gentler load on the model provider.
    ///
    /// Wall clock is what this costs: a case spends nearly all of its time waiting on the model, so
    /// a suite of forty 40-second cases takes half an hour serially and a few minutes at four at a
    /// time. Four rather than "as many as there are cases" because the limit that bites first is the
    /// provider's rate limit, and a 429 storm turns a slow run into a failed one.
    ///
    /// A stored suite written before this field existed has no value for it, which deserialises to
    /// this default -- so existing suites speed up without being touched. Set it to 1 for a suite
    /// whose cases must not overlap.
    /// </summary>
    public int MaxParallelCases { get; set; } = AgentTestConcurrency.Default;

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The one place that decides how many cases of a run execute at the same time, so the suite
/// default, the per-run override and the API's validation message can never drift apart.
/// </summary>
public static class AgentTestConcurrency
{
    /// <summary>Cases at a time when neither the run nor the suite says otherwise.</summary>
    public const int Default = 4;

    /// <summary>
    /// The highest value accepted. Not a technical ceiling -- the harness would happily open
    /// thirty conversations -- but past a handful the provider's rate limit, not the host, decides
    /// what happens, and a run that fails on 429s is worse than a slow one. Raise it deliberately.
    /// </summary>
    public const int Max = 16;

    /// <summary>
    /// The effective degree of parallelism for a run: the run's own value when it has one, else the
    /// suite's, else <see cref="Default"/>, clamped to [1, <see cref="Max"/>].
    ///
    /// Zero and negative mean "not set" rather than "no parallelism": that is what a run document
    /// written before this field existed deserialises to, and treating it as 0 would stop such a
    /// run from executing anything at all.
    /// </summary>
    public static int Resolve(int runValue, int suiteValue)
    {
        var chosen = runValue > 0 ? runValue : suiteValue > 0 ? suiteValue : Default;
        return Math.Clamp(chosen, 1, Max);
    }
}
