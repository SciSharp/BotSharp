using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Knowledges.Options;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeOrchestrator
{
    string KnowledgeType { get; }

    #region Collection
    Task<bool> ExistCollection(string collectionName, KnowledgeCollectionOptions options)
        => Task.FromResult(false);
    Task<bool> CreateCollection(string collectionName, CollectionCreateOptions options)
        => Task.FromResult(false);
    Task<bool> DeleteCollection(string collectionName, KnowledgeCollectionOptions options)
        => Task.FromResult(false);
    Task<IEnumerable<KnowledgeCollectionConfig>> GetCollections(KnowledgeCollectionOptions options)
        => Task.FromResult(Enumerable.Empty<KnowledgeCollectionConfig>());
    Task<KnowledgeCollectionDetails?> GetCollectionDetails(string collectionName, KnowledgeCollectionOptions options)
        => Task.FromResult<KnowledgeCollectionDetails?>(null);
    #endregion

    #region Data
    Task<IEnumerable<KnowledgeSearchResult>> Search(string query, string collectionName, KnowledgeSearchOptions options)
        => Task.FromResult(Enumerable.Empty<KnowledgeSearchResult>());
    Task<StringIdPagedItems<KnowledgeSearchResult>> GetPagedCollectionData(string collectionName, KnowledgeFilter filter)
        => Task.FromResult(new StringIdPagedItems<KnowledgeSearchResult>());
    Task<IEnumerable<KnowledgeCollectionData>> GetCollectionData(string collectionName, IEnumerable<string> ids, KnowledgeQueryOptions? options = null)
        => Task.FromResult(Enumerable.Empty<KnowledgeCollectionData>());
    Task<bool> DeleteCollectionData(string collectionName, string id, KnowledgeCollectionOptions? options)
        => Task.FromResult(false);
    Task<bool> DeleteCollectionData(string collectionName, KnowledgeCollectionOptions? options)
        => Task.FromResult(false);
    Task<bool> CreateCollectionData(string collectionName, KnowledgeCreateModel create)
        => Task.FromResult(false);
    Task<bool> UpdateCollectionData(string collectionName, KnowledgeUpdateModel update)
        => Task.FromResult(false);
    Task<bool> UpsertCollectionData(string collectionName, KnowledgeUpdateModel update)
        => Task.FromResult(false);
    #endregion

    #region Index
    Task<SuccessFailResponse<string>> CreateIndexes(string collectionName, KnowledgeIndexOptions options)
        => Task.FromResult(new SuccessFailResponse<string>());
    Task<SuccessFailResponse<string>> DeleteIndexes(string collectionName, KnowledgeIndexOptions options)
        => Task.FromResult(new SuccessFailResponse<string>());
    #endregion

    #region Snapshot
    Task<IEnumerable<KnowledgeCollectionSnapshot>> GetCollectionSnapshots(string collectionName, KnowledgeSnapshotOptions? options = null)
        => Task.FromResult(Enumerable.Empty<KnowledgeCollectionSnapshot>());
    Task<KnowledgeCollectionSnapshot?> CreateCollectionSnapshot(string collectionName, KnowledgeSnapshotOptions? options = null)
        => Task.FromResult<KnowledgeCollectionSnapshot?>(null);
    Task<BinaryData> DownloadCollectionSnapshot(string collectionName, string snapshotFileName, KnowledgeSnapshotOptions? options = null)
        => Task.FromResult(new BinaryData(Array.Empty<byte>()));
    Task<bool> RecoverCollectionFromSnapshot(string collectionName, string snapshotFileName, BinaryData snapshotData, KnowledgeSnapshotOptions? options = null)
        => Task.FromResult(false);
    Task<bool> DeleteCollectionSnapshot(string collectionName, string snapshotName, KnowledgeSnapshotOptions? options = null)
        => Task.FromResult(false);
    #endregion
}
