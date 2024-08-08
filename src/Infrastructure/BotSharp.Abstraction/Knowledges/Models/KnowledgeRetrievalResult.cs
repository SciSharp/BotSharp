namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeRetrievalResult
{
    public string Id { get; set; }
    public string Text { get; set; }
    public float Score { get; set; }
    public float[]? Vector { get; set; }
}
