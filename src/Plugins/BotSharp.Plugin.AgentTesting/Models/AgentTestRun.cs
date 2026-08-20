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
