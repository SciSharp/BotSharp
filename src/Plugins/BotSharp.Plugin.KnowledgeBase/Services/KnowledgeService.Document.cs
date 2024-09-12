using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using System.Net.Http;
using System.Net.Mime;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<UploadKnowledgeResponse> UploadKnowledgeDocuments(string collectionName, IEnumerable<ExternalFileModel> files)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || files.IsNullOrEmpty())
        {
            return new UploadKnowledgeResponse
            {
                Success = [],
                Failed = files?.Select(x => x.FileName) ?? new List<string>()
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
                // Get document info
                var (contentType, bytes) = await GetFileInfo(file);
                var contents = await GetFileContent(contentType, bytes);
                
                // Save document
                var fileId = Guid.NewGuid().ToString();
                var saved = SaveDocument(collectionName, vectorStoreProvider, fileId, file.FileName, bytes);
                if (!saved)
                {
                    failedFiles.Add(file.FileName);
                    continue;
                }

                // Save to vector db
                var dataIds = await SaveToVectorDb(collectionName, fileId, file.FileName, contents);
                if (!dataIds.IsNullOrEmpty())
                {
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

            // Get doc meta data
            var metaData = fileStorage.GetKnowledgeBaseFileMeta(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);
            // Delete doc
            fileStorage.DeleteKnowledgeFile(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);

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

        // Get doc meta data
        var files = fileStorage.GetKnowledgeBaseFiles(collectionName.CleanStr(), vectorStoreProvider.CleanStr());
        return files;
    }

    public async Task<FileBinaryDataModel?> GetKnowledgeDocumentBinaryData(string collectionName, string fileId)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;

        // Get doc binary data
        var file = fileStorage.GetKnowledgeBaseFileBinaryData(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId);
        return file;
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

    private async Task<IEnumerable<string>> GetFileContent(string contentType, byte[] bytes)
    {
        var results = new List<string>();

        if (contentType.IsEqualTo(MediaTypeNames.Text.Plain))
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            reader.Close();
            stream.Close();

            var lines = TextChopper.Chop(content, new ChunkOption
            {
                Size = 1024,
                Conjunction = 32,
                SplitByWord = true,
            });
            results.AddRange(lines);
        }
        else if (contentType.IsEqualTo(MediaTypeNames.Application.Pdf))
        {
            // to do
        }
        
        return results;
    }

    private bool SaveDocument(string collectionName, string vectorStoreProvider, string fileId, string fileName, byte[] bytes)
    {
        var fileStoreage = _services.GetRequiredService<IFileStorageService>();
        var data = BinaryData.FromBytes(bytes);
        var saved = fileStoreage.SaveKnowledgeBaseFile(collectionName.CleanStr(), vectorStoreProvider.CleanStr(), fileId, fileName, data);
        return saved;
    }

    private async Task<IEnumerable<string>> SaveToVectorDb(string collectionName, string fileId, string fileName, IEnumerable<string> contents)
    {
        if (contents.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }

        var dataIds = new List<string>();
        var vectorDb = GetVectorDb();
        var textEmbedding = GetTextEmbedding(collectionName);

        for (int i = 0; i < contents.Count(); i++)
        {
            var content = contents.ElementAt(i);
            var vector = await textEmbedding.GetVectorAsync(content);
            var dataId = Guid.NewGuid();
            var saved = await vectorDb.Upsert(collectionName, dataId, vector, content, new Dictionary<string, string>
            {
                { "fileName", fileName },
                { "fileId", fileId },
                { "textNumber", $"{i + 1}" }
            });

            if (!saved) continue;

            dataIds.Add(dataId.ToString());
        }

        return dataIds;
    }
    #endregion
}
