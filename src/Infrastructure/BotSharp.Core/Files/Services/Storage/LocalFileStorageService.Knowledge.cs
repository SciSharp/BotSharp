using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public bool SaveKnowledgeFiles(string collectionName, string fileId, string fileName, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(fileId))
        {
            return false;
        }

        try
        {
            var dir = Path.Combine(_baseDir, KNOWLEDGE_FOLDER, KNOWLEDGE_DOC_FOLDER, collectionName, fileId);
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
            _logger.LogWarning($"Error when saving knowledge file (Collection: {collectionName}, File name: {fileName}). {ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }
}
