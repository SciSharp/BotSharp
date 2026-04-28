namespace BotSharp.Abstraction.Graph.Requests;

public class GraphEdgeUpdateRequest
{
    public string Id { get; set; } = null!;
    public Dictionary<string, object>? Properties { get; set; }
}
