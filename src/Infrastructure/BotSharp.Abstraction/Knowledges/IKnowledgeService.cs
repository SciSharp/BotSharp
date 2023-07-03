using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task Feed(KnowledgeFeedModel knowledge);
    Task<string> GetKnowledges(KnowledgeRetrievalModel retrievalModel);
    Task<string> GetAnswer(KnowledgeRetrievalModel retrievalModel);
}
