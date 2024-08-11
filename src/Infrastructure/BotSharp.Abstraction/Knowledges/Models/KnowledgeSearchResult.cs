namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeSearchResult
{
    public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    public double Score { get; set; }
    public float[]? Vector { get; set; }
}

public class KnowledgeRetrievalResult : KnowledgeSearchResult
{
}