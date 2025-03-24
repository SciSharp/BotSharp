using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    #region Vector
    Task<bool> ExistVectorCollection(string collectionName);
    Task<bool> CreateVectorCollection(string collectionName, string collectionType, int dimension, string provider, string model);
    Task<bool> DeleteVectorCollection(string collectionName);
    Task<IEnumerable<VectorCollectionConfig>> GetVectorCollections(string? type = null);
    Task<IEnumerable<VectorSearchResult>> SearchVectorKnowledge(string query, string collectionName, VectorSearchOptions options);
    Task<StringIdPagedItems<VectorSearchResult>> GetPagedVectorCollectionData(string collectionName, VectorFilter filter);
    Task<bool> DeleteVectorCollectionData(string collectionName, string id);
    Task<bool> DeleteVectorCollectionAllData(string collectionName);
    Task<bool> CreateVectorCollectionData(string collectionName, VectorCreateModel create);
    Task<bool> UpdateVectorCollectionData(string collectionName, VectorUpdateModel update);
    Task<bool> UpsertVectorCollectionData(string collectionName, VectorUpdateModel update);
    #endregion

    #region Graph
    Task<GraphSearchResult> SearchGraphKnowledge(string query, GraphSearchOptions options);
    #endregion

    #region Document
    /// <summary>
    /// Save documents and their contents to knowledgebase
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="files"></param>
    /// <returns></returns>
    Task<UploadKnowledgeResponse> UploadDocumentsToKnowledge(string collectionName, IEnumerable<ExternalFileModel> files, ChunkOption? option = null);
    /// <summary>
    /// Save document content to knowledgebase without saving the document
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileName"></param>
    /// <param name="fileSource"></param>
    /// <param name="contents"></param>
    /// <param name="refData"></param>
    /// <returns></returns>
    Task<bool> ImportDocumentContentToKnowledge(string collectionName, string fileName, string fileSource, IEnumerable<string> contents,
        DocMetaRefData? refData = null, Dictionary<string, object>? payload = null);
    /// <summary>
    /// Delete one document and its related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeDocument(string collectionName, Guid fileId);
    /// <summary>
    /// Delete all documents and their related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter);
    Task<PagedItems<KnowledgeFileModel>> GetPagedKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter);
    Task<FileBinaryDataModel> GetKnowledgeDocumentBinaryData(string collectionName, Guid fileId);
    #endregion

    #region Snapshot
    Task<IEnumerable<VectorCollectionSnapshot>> GetVectorCollectionSnapshots(string collectionName);
    Task<VectorCollectionSnapshot?> CreateVectorCollectionSnapshot(string collectionName);
    Task<BinaryData> DownloadVectorCollectionSnapshot(string collectionName, string snapshotFileName);
    Task<bool> RecoverVectorCollectionFromSnapshot(string collectionName, string snapshotFileName, BinaryData snapshotData);
    Task<bool> DeleteVectorCollectionSnapshot(string collectionName, string snapshotName);
    #endregion

    #region Common
    Task<bool> RefreshVectorKnowledgeConfigs(VectorCollectionConfigsModel configs);
    #endregion
}
