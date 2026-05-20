using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public bool SaveKnowledgeBaseFile(string collectionName, string knowledgebaseProvider, Guid fileId, string fileName, BinaryData fileData)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(knowledgebaseProvider))
        {
            return false;
        }

        try
        {
            var docDir = BuildKnowledgeCollectionFileDir(collectionName, knowledgebaseProvider);
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
            _logger.LogWarning(ex, $"Error when saving knowledge file " +
                $"(Vector store provider: {knowledgebaseProvider}, Collection: {collectionName}, File name: {fileName}).");
            return false;
        }
    }

    public bool DeleteKnowledgeFile(string collectionName, string knowledgebaseProvider, Guid? fileId = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(knowledgebaseProvider))
        {
            return false;
        }

        var dir = BuildKnowledgeCollectionFileDir(collectionName, knowledgebaseProvider);
        if (!ExistDirectory(dir))
        {
            return false;
        }

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

    public string GetKnowledgeBaseFileUrl(string collectionName, string knowledgebaseProvider, Guid fileId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(knowledgebaseProvider)
            || string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var docDir = BuildKnowledgeCollectionFileDir(collectionName, knowledgebaseProvider);
        var file = Path.Combine(docDir, fileId.ToString(), fileName);
        if (!File.Exists(file))
        {
            return string.Empty;
        }

        return $"/knowledge/document/{collectionName}/file/{fileId}";
    }

    public BinaryData GetKnowledgeBaseFileBinaryData(string collectionName, string knowledgebaseProvider, Guid fileId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(knowledgebaseProvider)
            || string.IsNullOrWhiteSpace(fileName))
        {
            return BinaryData.Empty;
        }

        var docDir = BuildKnowledgeCollectionFileDir(collectionName, knowledgebaseProvider);
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
    private string BuildKnowledgeCollectionFileDir(string collectionName, string knowledgebaseProvider)
    {
        return Path.Combine(_baseDir, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, knowledgebaseProvider.CleanStr(), collectionName.CleanStr());
    }
    #endregion
}
