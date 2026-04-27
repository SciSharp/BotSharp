namespace BotSharp.Abstraction.Graph.Requests;

public class GraphNodeUpdateRequest
{
    public string Id { get; set; } = null!;
    public string[]? Labels { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public DateTime? Time { get; set; }
}
