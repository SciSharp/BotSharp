using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Knowledges.Options;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeOrchestrator
{
    string Type { get; }

    #region Collection
    Task<bool> ExistCollection(string collectionName, KnowledgeCollectionOptions options);
    Task<bool> CreateCollection(string collectionName, CollectionCreateOptions options);
    Task<bool> DeleteCollection(string collectionName, KnowledgeCollectionOptions options);
    Task<IEnumerable<KnowledgeCollectionConfig>> GetCollections(KnowledgeCollectionOptions options);
    #endregion

    #region Data
    Task<IEnumerable<KnowledgeSearchResult>> Search(string query, string collectionName, KnowledgeSearchOptions options);
    Task<StringIdPagedItems<KnowledgeSearchResult>> GetPagedCollectionData(string collectionName, KnowledgeFilter filter);
    Task<IEnumerable<KnowledgeCollectionData>> GetCollectionData(string collectionName, IEnumerable<string> ids, KnowledgeQueryOptions? options = null);
    Task<bool> DeleteCollectionData(string collectionName, string id, KnowledgeCollectionOptions? options);
    Task<bool> DeleteCollectionData(string collectionName, KnowledgeCollectionOptions? options);
    Task<bool> CreateCollectionData(string collectionName, KnowledgeCreateModel create);
    Task<bool> UpdateCollectionData(string collectionName, KnowledgeUpdateModel update);
    Task<bool> UpsertCollectionData(string collectionName, KnowledgeUpdateModel update);
    #endregion

    #region Index
    Task<SuccessFailResponse<string>> CreateIndexes(string collectionName, KnowledgeIndexOptions options);
    Task<SuccessFailResponse<string>> DeleteIndexes(string collectionName, KnowledgeIndexOptions options);
    #endregion

    #region Snapshot
    Task<IEnumerable<KnowledgeCollectionSnapshot>> GetCollectionSnapshots(string collectionName, KnowledgeSnapshotOptions? options = null);
    Task<KnowledgeCollectionSnapshot?> CreateCollectionSnapshot(string collectionName, KnowledgeSnapshotOptions? options = null);
    Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName, KnowledgeSnapshotOptions? options = null);
    Task<bool> RecoverCollectionFromSnapshot(string collectionName, string snapshotFileName, BinaryData snapshotData, KnowledgeSnapshotOptions? options = null);
    Task<bool> DeleteCollectionSnapshot(string collectionName, string snapshotName, KnowledgeSnapshotOptions? options = null);
    #endregion
}
