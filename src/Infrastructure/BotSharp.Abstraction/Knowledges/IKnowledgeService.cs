namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task<IEnumerable<string>> GetKnowledgeCollections();
    Task<IEnumerable<KnowledgeSearchResult>> SearchKnowledge(string collectionName, KnowledgeSearchOptions options);
    Task FeedKnowledge(string collectionName, KnowledgeCreationModel model);
    Task<StringIdPagedItems<KnowledgeSearchResult>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter);
    Task<bool> DeleteKnowledgeCollectionData(string collectionName, string id);
}
