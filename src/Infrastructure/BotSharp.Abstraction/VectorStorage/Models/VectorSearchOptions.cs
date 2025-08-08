using BotSharp.Abstraction.Knowledges.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorSearchOptions
{
    public IEnumerable<string>? Fields { get; set; } = [KnowledgePayloadName.Text, KnowledgePayloadName.Answer];
    public IEnumerable<VectorFilterGroup>? FilterGroups { get; set; }
    public int? Limit { get; set; } = 5;
    public float? Confidence { get; set; } = 0.5f;
    public bool WithVector { get; set; }

    public static VectorSearchOptions Default()
    {
        return new()
        {
            Fields = [KnowledgePayloadName.Text, KnowledgePayloadName.Answer],
            FilterGroups = null,
            Limit = 5,
            Confidence = 0.5f,
            WithVector = false
        };
    }
}
