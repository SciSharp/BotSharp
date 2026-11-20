using System.Threading;

namespace BotSharp.Abstraction.Graph.Options;

public class GraphQueryOptions : GraphQueryExecuteOptions
{
    public string Provider { get; set; }
}

public class GraphQueryExecuteOptions
{
    public string? GraphId { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
    public string? Method { get; set; }

    /// <summary>
    /// Upper bound for the whole query, retries included. Null keeps the provider's own timeout.
    /// Set it when the caller is itself waited on with a deadline, so the query gives up before the caller does.
    /// A provider's own HTTP timeout may still end a single attempt sooner.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Cancels the query, retries included, e.g. when the request that needs the result is aborted.
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; }
}