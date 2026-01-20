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
}