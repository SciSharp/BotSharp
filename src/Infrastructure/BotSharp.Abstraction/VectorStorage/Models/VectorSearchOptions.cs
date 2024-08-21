using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorSearchOptions
{
    public IEnumerable<string>? Fields { get; set; } = new List<string> { KnowledgePayloadName.Text, KnowledgePayloadName.Answer };
    public int? Limit { get; set; } = 5;
    public float? Confidence { get; set; } = 0.5f;
    public bool WithVector { get; set; }
}
