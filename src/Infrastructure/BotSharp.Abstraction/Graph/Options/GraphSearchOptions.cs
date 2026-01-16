namespace BotSharp.Abstraction.Graph.Options;

public class GraphSearchOptions
{
    public string? Provider { get; set; }
    public string? GraphId { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
    public string? Method { get; set; }
}
