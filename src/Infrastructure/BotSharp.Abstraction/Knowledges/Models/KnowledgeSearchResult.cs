namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeSearchResult
{
    public Dictionary<string, string> Data { get; set; } = new();
    public double Score { get; set; }
    public float[]? Vector { get; set; }
}

public class KnowledgeRetrievalResult : KnowledgeSearchResult
{
}