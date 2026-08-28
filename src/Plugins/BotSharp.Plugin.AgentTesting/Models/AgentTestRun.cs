using MongoDB.Bson.Serialization.Attributes;

namespace BotSharp.Plugin.AgentTesting.Models;

/// <summary>
/// One model to run against. Absent/empty means "use the agent's own LlmConfig" -- which was the
/// only behaviour before multi-model existed, so a historical Run document without this field keeps
/// its original meaning and needs no migration.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public class TestModel
{
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;

    /// <summary>"provider/model" -- used for logging and for de-duplicating result columns.</summary>
    public override string ToString() => $"{Provider}/{Model}";
}

public class AgentTestRun : MongoBase
{
    public string SuiteId { get; set; } = default!;

    /// <summary>See <see cref="AgentTestStatus"/>.</summary>
    public string Status { get; set; } = AgentTestStatus.Pending;

    public string? TriggeredBy { get; set; }

    /// <summary>
    /// Run only these case ids; null/empty means every enabled case in the suite (the original
    /// behaviour). This is what makes "re-run just the failures" -- a core regression-harness
    /// scenario -- possible. Mongo is schemaless, so no migration was needed to add it.
    /// </summary>
    public List<string>? CaseIds { get; set; }

    /// <summary>
    /// Models this run sweeps. Null/empty = a single pass on the agent's own LlmConfig (the
    /// behaviour from before multi-model). When set, the executor runs the cartesian product of
    /// cases x models and every AgentTestCaseResult records which model produced it -- so
    /// TotalCount is "cases x models" and no longer equals the case count.
    /// </summary>
    public List<TestModel>? Models { get; set; }

    public int TotalCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>
    /// Routing accuracy for this run, one row per model swept, counting only cases whose CaseType is
    /// Routing.
    ///
    /// Kept per model rather than as one run-wide number because that is the whole point of a
    /// comparison run: a single figure covering every model averages the candidate together with the
    /// baseline and hides exactly the difference the run exists to measure.
    ///
    /// A summary of the case results, not a second source of truth -- the AgentTestCaseResult rows
    /// remain authoritative and carry CaseType themselves. It lives here so the run LIST can show
    /// the figure without loading every result of every run.
    /// </summary>
    public List<RoutingAccuracy> RoutingAccuracies { get; set; } = [];

    /// <summary>
    /// Latency, token and cost figures for this run, one row per model swept. Computed once when the
    /// run finishes rather than accumulated per case, because a percentile cannot be updated
    /// incrementally -- it needs every value at once.
    /// </summary>
    public List<PerformanceSummary> PerformanceSummaries { get; set; } = [];

    /// <summary>
    /// The unit costs actually in force for each model when this run executed.
    ///
    /// A cost figure is meaningless without them: a provider price change makes this run's cost
    /// incomparable with an earlier one's, and a run that only recorded a version STRING would leave
    /// nobody able to check whether two versions differ. Snapshotting the numbers makes that
    /// checkable instead of a matter of trust.
    /// </summary>
    public List<ModelPricingSnapshot> ModelPricing { get; set; } = [];

    /// <summary>
    /// Why a run ended as <see cref="AgentTestStatus.Error"/> -- an infrastructure stop that
    /// happened before or instead of executing cases (suite gone, suite disabled, the CaseIds
    /// filter matched nothing, the host died mid-run, an unhandled exception).
    ///
    /// Distinct from AgentTestCaseResult.Error, which explains one case. A run can fail with ZERO
    /// case results, and until this field existed the reason lived only in the server log: the API
    /// returned status=Error with 0/0/0/0 and an empty result list, so no UI could ever say why.
    /// </summary>
    public string? Error { get; set; }

    public bool CancelRequested { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Latency, tokens and cost for one model within a run. Sums and percentiles are stored; averages are
/// not, because an average is Total/CaseCount and a stored copy is one more thing that can disagree
/// with the rows it came from.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public class PerformanceSummary
{
    /// <summary>Null for both when the run swept no models and used each agent's own LlmConfig.</summary>
    public string? Provider { get; set; }
    public string? Model { get; set; }

    /// <summary>
    /// Results this row covers. Only cases that actually executed: an Error case that never reached
    /// the model would drag a latency percentile towards zero and make a broken run look fast.
    /// </summary>
    public int CaseCount { get; set; }

    /// <summary>Median and 95th percentile of AgentTestCaseResult.ModelDurationMs.</summary>
    public long LatencyP50Ms { get; set; }
    public long LatencyP95Ms { get; set; }

    public long TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

/// <summary>
/// One model's configured unit costs at the moment a run executed. Text tokens only -- the harness
/// drives text conversations, and carrying audio and image tiers that are always zero here would
/// suggest they had been checked.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public class ModelPricingSnapshot
{
    public string? Provider { get; set; }
    public string? Model { get; set; }

    /// <summary>Null when the model's settings could not be read, which is itself worth recording.</summary>
    public float? TextInputCost { get; set; }
    public float? TextOutputCost { get; set; }
}

/// <summary>
/// How many Routing cases one model got right in a run. Stored as counts, never as a percentage:
/// a stored ratio would go stale the moment another case result arrives, and "3/4" says something
/// "75%" does not -- how much the figure is worth trusting.
/// </summary>
[BsonIgnoreExtraElements(Inherited = true)]
public class RoutingAccuracy
{
    /// <summary>Null for both when the run swept no models and used each agent's own LlmConfig.</summary>
    public string? Provider { get; set; }
    public string? Model { get; set; }

    /// <summary>Routing cases executed under this model, whatever their outcome.</summary>
    public int CaseCount { get; set; }

    /// <summary>
    /// Of those, how many passed. A routing case passes only when its routing assertions held, so
    /// Passed here means the conversation reached the expected agent. Error rows (a timeout, a dead
    /// canary) count towards CaseCount but never towards PassedCount -- treating "could not tell"
    /// as correct is how a broken harness starts reporting perfect accuracy.
    /// </summary>
    public int PassedCount { get; set; }
}
