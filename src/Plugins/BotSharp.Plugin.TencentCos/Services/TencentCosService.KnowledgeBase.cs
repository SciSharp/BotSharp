using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, string fileId, string fileName, Stream stream)
    {
        throw new NotImplementedException();
    }

    public bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, string? fileId = null)
    {
        throw new NotImplementedException();
    }

    public bool SaveKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, string fileId, KnowledgeDocMetaData metaData)
    {
        throw new NotImplementedException();
    }

    public KnowledgeDocMetaData? GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, string fileId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<KnowledgeFileModel> GetKnowledgeBaseFiles(string collectionName, string vectorStoreProvider)
    {
        throw new NotImplementedException();
    }

    public FileBinaryDataModel? GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, string fileId)
    {
        throw new NotImplementedException();
    }
}
