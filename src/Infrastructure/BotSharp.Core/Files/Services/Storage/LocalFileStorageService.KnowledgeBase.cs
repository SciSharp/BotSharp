using BotSharp.Abstraction.Knowledges.Models;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, string fileId, string fileName, BinaryData fileData)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileId))
        {
            return false;
        }

        try
        {
            var docDir = BuildKnowledgeCollectionDocumentDir(collectionName, vectorStoreProvider);
            var dir = Path.Combine(docDir, fileId);
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

    public bool DeleteKnowledgeFile(string collectionName, string vectorStoreProvider, string? fileId = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider))
        {
            return false;
        }

        var dir = BuildKnowledgeCollectionDocumentDir(collectionName, vectorStoreProvider);
        if (!ExistDirectory(dir)) return false;

        if (string.IsNullOrEmpty(fileId))
        {
            Directory.Delete(dir, true);
        }
        else
        {
            var fileDir = Path.Combine(dir, fileId);
            if (ExistDirectory(fileDir))
            {
                Directory.Delete(fileDir, true);
            }
        }

        return true;
    }

    public string GetKnowledgeBaseFileUrl(string collectionName, string fileId)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
             || string.IsNullOrWhiteSpace(fileId))
        {
            return string.Empty;
        }

        return $"/knowledge/document/{collectionName}/file/{fileId}";
    }

    public FileBinaryDataModel? GetKnowledgeBaseFileBinaryData(string collectionName, string vectorStoreProvider, string fileId)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        var docDir = BuildKnowledgeCollectionDocumentDir(collectionName, vectorStoreProvider);
        var fileDir = Path.Combine(docDir, fileId);
        if (!ExistDirectory(fileDir)) return null;

        var metaFile = Path.Combine(fileDir, KNOWLEDGE_DOC_META_FILE);
        var content = File.ReadAllText(metaFile);
        var metaData = JsonSerializer.Deserialize<KnowledgeDocMetaData>(content, _jsonOptions);
        var file = Path.Combine(fileDir, metaData.FileName);
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        stream.Position = 0;

        return new FileBinaryDataModel
        {
            FileName = metaData.FileName,
            ContentType = metaData.ContentType,
            FileBinaryData = BinaryData.FromStream(stream)
        };
    }


    #region Private methods
    private string BuildKnowledgeCollectionDocumentDir(string collectionName, string vectorStoreProvider)
    {
        return Path.Combine(_baseDir, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, vectorStoreProvider, collectionName);
    }
    #endregion
}
