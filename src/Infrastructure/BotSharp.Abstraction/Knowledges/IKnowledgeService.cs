namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task<IEnumerable<KnowledgeRetrievalResult>> SearchKnowledge(string collectionName, KnowledgeRetrievalOptions options);
    Task FeedKnowledge(string collectionName, KnowledgeCreationModel model);
    Task<StringIdPagedItems<KnowledgeCollectionData>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter);
    Task<bool> DeleteKnowledgeCollectionData(string collectionName, string id);
}
