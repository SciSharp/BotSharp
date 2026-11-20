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
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}