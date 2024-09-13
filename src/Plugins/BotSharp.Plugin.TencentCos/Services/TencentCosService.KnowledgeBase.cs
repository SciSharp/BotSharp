namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, string fileId, string fileName, BinaryData fileData)
    {
        throw new NotImplementedException();
    }

    public bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, string? fileId = null)
    {
        throw new NotImplementedException();
    }

    public string GetKnowledgeBaseFileUrl(string collectionName, string fileId)
    {
        throw new NotImplementedException();
    }

    public FileBinaryDataModel? GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, string fileId)
    {
        throw new NotImplementedException();
    }
}
