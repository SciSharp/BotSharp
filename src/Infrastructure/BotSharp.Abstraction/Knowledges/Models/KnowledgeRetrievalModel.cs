using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeRetrievalModel
{
    public string Collection { get; set; } = KnowledgeCollectionName.BotSharp;
    public string Text { get; set; } = string.Empty;
    public IEnumerable<string>? Fields { get; set; } = new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer };
    public int? Limit { get; set; } = 5;
    public float? Confidence { get; set; } = 0.5f;
    public bool WithVector { get; set; }
}
