namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeCollectionData
{
    public string Id { get; set; }
    public string Question { get; set; }
    public string Answer { get; set; }
    public float[]? Vector { get; set; }
}
