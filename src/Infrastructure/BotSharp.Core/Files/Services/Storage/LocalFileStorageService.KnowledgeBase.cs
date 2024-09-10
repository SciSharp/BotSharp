using BotSharp.Abstraction.Knowledges.Models;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string vectorStoreProvider, string fileId, string fileName, Stream stream)
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
            using var fs = File.Create(filePath);
            stream.CopyTo(fs);
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

    public bool SaveKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, string fileId, KnowledgeDocMetaData metaData)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(vectorStoreProvider)
            || string.IsNullOrWhiteSpace(fileId))
        {
            return false;
        }

        var docDir = BuildKnowledgeCollectionDocumentDir(collectionName, vectorStoreProvider);
        var dir = Path.Combine(docDir, fileId);
        if (!ExistDirectory(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var metaFile = Path.Combine(dir, KNOWLEDGE_DOC_META_FILE);
        var content = JsonSerializer.Serialize(metaData, _jsonOptions);
        File.WriteAllText(metaFile, content);
        return true;
    }

    public KnowledgeDocMetaData? GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, string fileId)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        var docDir = BuildKnowledgeCollectionDocumentDir(collectionName, vectorStoreProvider);
        var metaFile = Path.Combine(docDir, fileId, KNOWLEDGE_DOC_META_FILE);
        if (!File.Exists(metaFile))
        {
            return null;
        }

        var content = File.ReadAllText(metaFile);
        var metaData = JsonSerializer.Deserialize<KnowledgeDocMetaData>(content, _jsonOptions);
        return metaData;
    }

    private string BuildKnowledgeCollectionDocumentDir(string collectionName, string vectorStoreProvider)
    {
        return Path.Combine(_baseDir, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, vectorStoreProvider, collectionName);
    }
}
