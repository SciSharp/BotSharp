namespace BotSharp.Abstraction.Graph.Requests;

public class GraphNodeCreationRequest
{
    public string? Id { get; set; }
    public string[]? Labels { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public DateTime? Time { get; set; }
}
