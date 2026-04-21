using BotSharp.Abstraction.Knowledges.Filters;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Responses;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeFileOrchestrator
{
    string Provider { get; }

    /// <summary>
    /// Save files and their contents to knowledgebase
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="files"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<UploadKnowledgeResponse> UploadFilesToKnowledge(string collectionName, IEnumerable<ExternalFileModel> files, KnowledgeFileHandleOptions? options = null);
    
    /// <summary>
    /// Delete one file and its related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileId"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeFile(string collectionName, Guid fileId, KnowledgeFileOptions? options = null);
    
    /// <summary>
    /// Delete all files and their related knowledge in the collection
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<bool> DeleteKnowledgeFiles(string collectionName, KnowledgeFileFilter filter);

    /// <summary>
    /// Get knowledge files by pagination
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<PagedItems<KnowledgeFileModel>> GetPagedKnowledgeFiles(string collectionName, KnowledgeFileFilter filter);

    /// <summary>
    /// Get knowledge file binary data
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="fileId"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<FileBinaryDataModel> GetKnowledgeFileBinaryData(string collectionName, Guid fileId, KnowledgeFileOptions? options = null);
}
