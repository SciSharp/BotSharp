namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeExecuteResult : KnowledgeCollectionData
{
    public KnowledgeExecuteResult()
    {

    }

    public static KnowledgeExecuteResult CopyFrom(KnowledgeCollectionData data)
    {
        return new KnowledgeExecuteResult
        {
            Id = data.Id,
            Payload = data.Payload,
            Score = data.Score,
            Vector = data.Vector
        };
    }
}
