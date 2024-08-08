using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task<List<KnowledgeChunk>> CollectChunkedKnowledge();
    Task EmbedKnowledge(List<KnowledgeChunk> chunks);

    Task Feed(KnowledgeFeedModel knowledge);
    Task EmbedKnowledge(KnowledgeCreationModel knowledge);
    Task<string> GetKnowledges(KnowledgeRetrievalModel retrievalModel);
    Task<List<RetrievedResult>> GetAnswer(KnowledgeRetrievalModel retrievalModel);

    #region List
    Task<StringIdPagedItems<KnowledgeCollectionData>> GetKnowledgeCollectionData(string collectionName, KnowledgeFilter filter);
    Task<IEnumerable<KnowledgeCollectionData>> GetSimilarKnowledgeData(string collectionName, KnowledgeFilter filter);
    Task<bool> DeleteKnowledgeCollectionData(string collectionName, string id);
    #endregion
}
