namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCollectionData
{
    public string Id { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
    public double? Score { get; set; }
    public float[]? Vector { get; set; }
}