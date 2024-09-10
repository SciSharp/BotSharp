using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using System.Net.Http;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments(string collectionName, IEnumerable<ExternalFileModel> files)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return new UploadKnowledgeResponse
            {
                Success = [],
                Failed = files.Select(x => x.FileName)
            };
        }

        var fileStoreage = _services.GetRequiredService<IFileStorageService>();
        var userId = await GetUserId();
        var vectorStoreProvider = _settings.VectorDb.Provider;
        var successFiles = new List<string>();
        var failedFiles = new List<string>();

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileData)
                && string.IsNullOrWhiteSpace(file.FileUrl))
            {
                continue;
            }

            try
            {
                var dataIds = new List<string>();

                // Chop text (to do)
                var (contentType, bytes) = await GetFileInfo(file);
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Save file
                var fileId = Guid.NewGuid().ToString();
                var saved = fileStoreage.SaveKnowledgeBaseFile(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId, file.FileName, stream);
                reader.Close();
                stream.Close();

                if (!saved)
                {
                    failedFiles.Add(file.FileName);
                    continue;
                }

                // Text embedding
                var vectorDb = GetVectorDb();
                var textEmbedding = GetTextEmbedding(collectionName);
                var vector = await textEmbedding.GetVectorAsync(content);

                // Save to vector db
                var dataId = Guid.NewGuid();
                saved = await vectorDb.Upsert(collectionName, dataId, vector, content, new Dictionary<string, string>
                {
                    { "fileName", file.FileName },
                    { "fileId", fileId },
                    { "page", "0" }
                });

                if (saved)
                {
                    dataIds.Add(dataId.ToString());
                    fileStoreage.SaveKnolwedgeBaseFileMeta(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId, new KnowledgeDocMetaData
                    {
                        Collection = collectionName,
                        FileId = fileId,
                        FileName = file.FileName,
                        ContentType = contentType,
                        VectorDataIds = dataIds,
                        CreateDate = DateTime.UtcNow,
                        CreateUserId = userId
                    });
                    successFiles.Add(file.FileName);
                }
                else
                {
                    failedFiles.Add(file.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error when processing knowledge file ({file.FileName}). {ex.Message}\r\n{ex.InnerException}");
                failedFiles.Add(file.FileName);
                continue;
            }
        }

        return new UploadKnowledgeResponse
        {
            Success = successFiles,
            Failed = failedFiles
        };
    }


    public async Task<bool> DeleteKnowledgeDocument(string collectionName, string fileId)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(fileId))
        {
            return false;
        }

        try
        {
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var vectorDb = GetVectorDb();
            var vectorStoreProvider = _settings.VectorDb.Provider;

            fileStorage.DeleteKnowledgeFile(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);
            var metaData = fileStorage.GetKnowledgeBaseFileMeta(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);

            if (metaData != null && !metaData.VectorDataIds.IsNullOrEmpty())
            {
                var guids = metaData.VectorDataIds.Where(x => Guid.TryParse(x, out _)).Select(x => Guid.Parse(x)).ToList();
                await vectorDb.DeleteCollectionData(collectionName, guids);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when deleting knowledge document " +
                $"(Collection: {collectionName}, File id: {fileId})" +
                $"\r\n{ex.Message}\r\n{ex.InnerException}");
            return false;
        }
    }


    public async Task<IEnumerable<KnowledgeFileModel>> GetKnowledgeDocuments(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return Enumerable.Empty<KnowledgeFileModel>();
        }

        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;
        var files = fileStorage.GetKnowledgeBaseFiles(collectionName.CleanStr(), vectorStoreProvider.CleanStr());
        return files;
    }

    public async Task<FileBinaryDataModel?> GetKnowledgeDocumentBinaryData(string collectionName, string fileId)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;
        var file = fileStorage.GetKnowledgeBaseFileBinaryData(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);
        return file;
    }


    public async Task FeedVectorKnowledge(string collectionName, KnowledgeCreationModel knowledge)
    {
        var index = 0;
        var lines = TextChopper.Chop(knowledge.Content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 32,
            SplitByWord = true,
        });

        var db = GetVectorDb();
        var textEmbedding = GetTextEmbedding(collectionName);

        await db.CreateCollection(collectionName, textEmbedding.GetDimension());
        foreach (var line in lines)
        {
            var vec = await textEmbedding.GetVectorAsync(line);
            await db.Upsert(collectionName, Guid.NewGuid(), vec, line);
            index++;
            Console.WriteLine($"Saved vector {index}/{lines.Count}: {line}\n");
        }
    }

    #region Private methods
    /// <summary>
    /// Get file content type and file bytes
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private async Task<(string, byte[])> GetFileInfo(ExternalFileModel file)
    {
        if (file == null)
        {
            return (string.Empty, new byte[0]);
        }

        if (!string.IsNullOrWhiteSpace(file.FileUrl))
        {
            var http = _services.GetRequiredService<IHttpClientFactory>();
            var contentType = FileUtility.GetFileContentType(file.FileName);
            using var client = http.CreateClient();
            var bytes = await client.GetByteArrayAsync(file.FileUrl);
            return (contentType, bytes);
        }
        else if (!string.IsNullOrWhiteSpace(file.FileData))
        {
            var (contentType, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
            return (contentType, bytes);
        }

        return (string.Empty, new byte[0]);
    }
    #endregion
}
