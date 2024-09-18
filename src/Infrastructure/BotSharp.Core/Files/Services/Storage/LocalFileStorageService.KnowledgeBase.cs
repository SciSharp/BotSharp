using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
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
            var dir = Path.Combine(docDir, fileId.ToString());
            if (ExistDirectory(dir))
            {
                Directory.Delete(dir);
            }
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, fileName);
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var ds = fileData.ToStream();
            ds.CopyTo(fs);
            fs.Flush();
            return true;
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
            Directory.Delete(dir, true);
        }
        else
        {
            var fileDir = Path.Combine(dir, fileId.ToString());
            if (ExistDirectory(fileDir))
            {
                Directory.Delete(fileDir, true);
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

        var docDir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
        var file = Path.Combine(docDir, fileId.ToString(), fileName);
        if (!File.Exists(file))
        {
            return string.Empty;
        }

        return $"/knowledge/document/{collectionName}/file/{fileId}";
    }

    public BinaryData GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, Guid fileId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileName))
        {
            return BinaryData.Empty;
        }

        var docDir = BuildKnowledgeCollectionFileDir(collectionName, vectorStoreProvider);
        var file = Path.Combine(docDir, fileId.ToString(), fileName);

        if (!File.Exists(file))
        {
            return BinaryData.Empty;
        }
        
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        stream.Position = 0;

        return BinaryData.FromStream(stream);
    }


    #region Private methods
    private string BuildKnowledgeCollectionFileDir(string collectionName, string vectorStoreProvider)
    {
        return Path.Combine(_baseDir, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, vectorStoreProvider.CleanStr(), collectionName.CleanStr());
    }
    #endregion
}
