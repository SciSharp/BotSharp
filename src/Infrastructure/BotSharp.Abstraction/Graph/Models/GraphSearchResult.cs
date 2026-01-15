namespace BotSharp.Abstraction.Graph.Models;

public class GraphSearchResult
{
    public string Result { get; set; } = string.Empty;
    public string[] Keys { get; set; } = [];
    public Dictionary<string, object?>[] Values { get; set; } = [];
}
