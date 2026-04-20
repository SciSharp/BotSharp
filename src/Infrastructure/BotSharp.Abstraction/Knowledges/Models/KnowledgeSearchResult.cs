namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeSearchResult : KnowledgeCollectionData
{
    public KnowledgeSearchResult()
    {

    }

    public static KnowledgeSearchResult CopyFrom(KnowledgeCollectionData data)
    {
        return new KnowledgeSearchResult
        {
            Id = data.Id,
            Payload = data.Payload,
            Score = data.Score,
            Vector = data.Vector
        };
    }
}
