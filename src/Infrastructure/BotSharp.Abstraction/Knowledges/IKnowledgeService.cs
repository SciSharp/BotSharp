using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task<IEnumerable<KnowledgeRetrievalResult>> SearchKnowledge(KnowledgeRetrievalModel model);
    Task FeedKnowledge(KnowledgeCreationModel model);
    Task<StringIdPagedItems<KnowledgeCollectionData>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter);
    Task<bool> DeleteKnowledgeCollectionData(string collectionName, string id);
}
