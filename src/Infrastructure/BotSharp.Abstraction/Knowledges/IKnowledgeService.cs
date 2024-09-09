using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    #region Vector
    Task<bool> CreateVectorCollection(string collectionName, int dimension);
    Task<bool> DeleteVectorCollection(string collectionName);
    Task<IEnumerable<string>> GetVectorCollections();
    Task<IEnumerable<VectorSearchResult>> SearchVectorKnowledge(string query, string collectionName, VectorSearchOptions options);
    Task FeedVectorKnowledge(string collectionName, KnowledgeCreationModel model);
    Task<StringIdPagedItems<VectorSearchResult>> GetPagedVectorCollectionData(string collectionName, VectorFilter filter);
    Task<bool> DeleteVectorCollectionData(string collectionName, string id);
    Task<bool> CreateVectorCollectionData(string collectionName, VectorCreateModel create);
    Task<bool> UpdateVectorCollectionData(string collectionName, VectorUpdateModel update);
    #endregion

    #region Graph
    Task<GraphSearchResult> SearchGraphKnowledge(string query, GraphSearchOptions options);
    #endregion

    #region Document
    #endregion
}
