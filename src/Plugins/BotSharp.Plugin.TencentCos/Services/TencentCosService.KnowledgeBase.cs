namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, Guid fileId, string fileName, BinaryData fileData)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return false;
        }

        try
        {
            var docDir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
            var dir = $"{docDir}/{fileId}";
            if (ExistDirectory(dir))
            {
                _cosClient.BucketClient.DeleteDir(dir);
            }

            var file = $"{dir}/{fileName}";
            var res = _cosClient.BucketClient.UploadBytes(file, fileData.ToArray());
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving knowledge file " +
                $"(Vector store provider: {vectorStoreProvider}, Collection: {collectionName}, File name: {fileName})." +
                $"\r\n{ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }

    public bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, Guid? fileId = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return false;
        }

        var dir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
        if (!ExistDirectory(dir)) return false;

        if (fileId == null)
        {
            _cosClient.BucketClient.DeleteDir(dir);
        }
        else
        {
            var fileDir = $"{dir}/{fileId}";
            if (ExistDirectory(fileDir))
            {
                _cosClient.BucketClient.DeleteDir(fileDir);
            }
        }

        return true;
    }

    public string GetKnowledgeBaseFileUrl(string collectionName, string vectorStoreProvider, Guid fileId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var docDir = BuildKnowledgeCollectionFileDir(vectorStoreProvider, collectionName);
        var fileDir = $"{docDir}/{fileId}";
        if (!ExistDirectory(fileDir))
        {
            return string.Empty;
        }

        return $"https://{_fullBuketName}.cos.{_settings.Region}.myqcloud.com/{fileDir}/{fileName}"; ;
    }

    public BinaryData GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, Guid fileId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileName))
        {
            return BinaryData.Empty;
        }

        try
        {
            var docDir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
            var fileDir = $"{docDir}/{fileId}";
            if (!ExistDirectory(fileDir))
            {
                return BinaryData.Empty;
            }

            var file = $"{fileDir}/{fileName}";
            var bytes = _cosClient.BucketClient.DownloadFileBytes(file);
            if (bytes == null)
            {
                return BinaryData.Empty;
            }

            return BinaryData.FromBytes(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when downloading collection file ({collectionName}-{vectorStoreProvider}-{fileId}-{fileName})" +
                $"\r\n{ex.Message}\r\n{ex.InnerException}");
            return BinaryData.Empty;
        }
    }


    #region Private methods
    private string BuildKnowledgeCollectionFileDir(string collectionName, string vectorStoreProvider)
    {
        return $"{KNOWLEDGE_FOLDER}/{KNOWLEDGE_DOC_FOLDER}/{vectorStoreProvider.CleanStr()}/{collectionName.CleanStr()}";
    }
    #endregion
}
