using BotSharp.Abstraction.Knowledges.Filters;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Responses;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeDocOrchestrator
{
    string Provider { get; }

    /// <summary>
    /// Save documents and their contents to knowledgebase
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="files"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    Task<UploadKnowledgeResponse> UploadDocumentsToKnowledge(string collectionName, IEnumerable<ExternalFileModel> files, KnowledgeFileHandleOptions? options = null);
    
    /// <summary>
    /// Delete one document and its related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileId"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeDocument(string collectionName, Guid fileId, KnowledgeFileOptions? options = null);
    
    /// <summary>
    /// Delete all documents and their related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter);

    /// <summary>
    /// Get knowlege documents by pagination
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<PagedItems<KnowledgeFileModel>> GetPagedKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter);

    /// <summary>
    /// Get knowledge document binary data
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileId"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<FileBinaryDataModel> GetKnowledgeDocumentBinaryData(string collectionName, Guid fileId, KnowledgeFileOptions? options = null);
}
