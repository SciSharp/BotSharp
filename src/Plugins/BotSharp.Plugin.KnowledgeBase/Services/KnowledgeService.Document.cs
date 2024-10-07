using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Knowledges.Helpers;
using BotSharp.Abstraction.VectorStorage.Enums;
using System.Net.Http;
using System.Net.Mime;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<UploadKnowledgeResponse> UploadDocumentsToKnowledge(string collectionName, IEnumerable<ExternalFileModel> files)
    {
        var res = new UploadKnowledgeResponse
        {
            Success = [],
            Failed = files?.Select(x => x.FileName) ?? new List<string>()
        };

        if (string.IsNullOrWhiteSpace(collectionName) || files.IsNullOrEmpty())
        {
            return res;
        }

        var exist = await ExistVectorCollection(collectionName);
        if (!exist)
        {
            return res;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
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
                var fileId = Guid.NewGuid();
                var saved = SaveDocument(collectionName, vectorStoreProvider, fileId, file.FileName, bytes);
                if (!saved)
                {
                    failedFiles.Add(file.FileName);
                    continue;
                }

                // Save to vector db
                var payload = new Dictionary<string, object>()
                {
                    { KnowledgePayloadName.DataSource, VectorDataSource.File },
                    { KnowledgePayloadName.FileId, fileId.ToString() },
                    { KnowledgePayloadName.FileName, file.FileName },
                    { KnowledgePayloadName.FileSource, file.FileSource }
                };

                if (!string.IsNullOrWhiteSpace(file.FileUrl))
                {
                    payload[KnowledgePayloadName.FileUrl] = file.FileUrl;
                }

                var dataIds = await SaveToVectorDb(collectionName, fileId, file.FileName, contents, payload);
                if (!dataIds.IsNullOrEmpty())
                {
                    db.SaveKnolwedgeBaseFileMeta(new KnowledgeDocMetaData
                    {
                        Collection = collectionName,
                        FileId = fileId,
                        FileName = file.FileName,
                        FileSource = file.FileSource,
                        ContentType = contentType,
                        VectorStoreProvider = vectorStoreProvider,
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


    public async Task<bool> ImportDocumentContentToKnowledge(string collectionName, string fileName, string fileSource,
        IEnumerable<string> contents, DocMetaRefData? refData = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName)
            || string.IsNullOrWhiteSpace(fileName)
            || contents.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            var exist = await ExistVectorCollection(collectionName);
            if (!exist) return false;

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var userId = await GetUserId();
            var vectorStoreProvider = _settings.VectorDb.Provider;
            var fileId = Guid.NewGuid();
            var contentType = FileUtility.GetFileContentType(fileName);

            var payload = new Dictionary<string, object>()
            {
                { KnowledgePayloadName.DataSource, VectorDataSource.File },
                { KnowledgePayloadName.FileId, fileId.ToString() },
                { KnowledgePayloadName.FileName, fileName },
                { KnowledgePayloadName.FileSource, fileSource }
            };

            if (!string.IsNullOrWhiteSpace(refData?.Url))
            {
                payload[KnowledgePayloadName.FileUrl] = refData.Url;
            }

            var dataIds = await SaveToVectorDb(collectionName, fileId, fileName, contents, payload);
            db.SaveKnolwedgeBaseFileMeta(new KnowledgeDocMetaData
            {
                Collection = collectionName,
                FileId = fileId,
                FileName = fileName,
                FileSource = fileSource,
                ContentType = contentType,
                VectorStoreProvider = vectorStoreProvider,
                VectorDataIds = dataIds,
                RefData = refData,
                CreateDate = DateTime.UtcNow,
                CreateUserId = userId
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when importing doc content to knowledgebase ({collectionName}-{fileName})" +
                $"\r\n{ex.Message}" +
                $"\r\n{ex.InnerException}");
            return false;
        }
    }


    public async Task<bool> DeleteKnowledgeDocument(string collectionName, Guid fileId)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        try
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var vectorDb = GetVectorDb();
            var vectorStoreProvider = _settings.VectorDb.Provider;

            // Get doc meta data
            var pageData = db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, new KnowledgeFileFilter
            {
                Size = 1,
                FileIds = [ fileId ]
            });

            // Delete doc
            fileStorage.DeleteKnowledgeFile(collectionName, vectorStoreProvider, fileId);
            
            var found = pageData?.Items?.FirstOrDefault();
            if (found != null && !found.VectorDataIds.IsNullOrEmpty())
            {
                var guids = found.VectorDataIds.Where(x => Guid.TryParse(x, out _)).Select(x => Guid.Parse(x)).ToList();
                await vectorDb.DeleteCollectionData(collectionName, guids);
            }

            db.DeleteKnolwedgeBaseFileMeta(collectionName, vectorStoreProvider, fileId);
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

    public async Task<bool> DeleteKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return false;

        
        var pageSize = filter.Size;
        var innerFilter = new KnowledgeFileFilter
        {
            Page = 1,
            Size = pageSize,
            FileIds = filter.FileIds,
            FileNames = filter.FileNames,
            FileSources = filter.FileSources,
            ContentTypes = filter.ContentTypes
        };

        var pageData = await GetPagedKnowledgeDocuments(collectionName, innerFilter);

        var total = pageData.Count;
        if (total == 0) return false;

        var page = 1;
        var totalPages = total % pageSize == 0 ? total / pageSize : total / pageSize + 1;

        while (page <= totalPages)
        {
            if (page > 1)
            {
                pageData = await GetPagedKnowledgeDocuments(collectionName, innerFilter);
            }

            var fileIds = pageData.Items.Select(x => x.FileId).ToList();
            foreach (var fileId in fileIds)
            {
                try
                {
                    await DeleteKnowledgeDocument(collectionName, fileId);
                }
                catch
                {
                    continue;
                }
            }

            page++;
        }

        return true;
    }


    public async Task<PagedItems<KnowledgeFileModel>> GetPagedKnowledgeDocuments(string collectionName, KnowledgeFileFilter filter)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return new PagedItems<KnowledgeFileModel>();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;

        // Get doc meta data
        var pagedData = db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, filter);

        var files = pagedData.Items?.Select(x => new KnowledgeFileModel
        {
            FileId = x.FileId,
            FileName = x.FileName,
            FileSource = x.FileSource,
            FileExtension = Path.GetExtension(x.FileName),
            ContentType = x.ContentType,
            FileUrl = fileStorage.GetKnowledgeBaseFileUrl(collectionName, vectorStoreProvider, x.FileId, x.FileName),
            RefData = x.RefData
        })?.ToList() ?? new List<KnowledgeFileModel>();

        return new PagedItems<KnowledgeFileModel>
        {
            Items = files,
            Count = pagedData.Count
        };
    }

    public async Task<FileBinaryDataModel> GetKnowledgeDocumentBinaryData(string collectionName, Guid fileId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;

        // Get doc binary data
        var pageData = db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, new KnowledgeFileFilter
        {
            Size = 1,
            FileIds = [ fileId ]
        });

        var metaData = pageData?.Items?.FirstOrDefault();
        if (metaData == null)
        {
            return new FileBinaryDataModel
            {
                FileName = "error.txt",
                ContentType = "text/plain",
                FileBinaryData = BinaryData.Empty
            };
        };

        var binaryData = fileStorage.GetKnowledgeBaseFileBinaryData(collectionName, vectorStoreProvider, fileId, metaData.FileName);
        return new FileBinaryDataModel
        {
            FileName = metaData.FileName,
            ContentType = metaData.ContentType,
            FileBinaryData = binaryData
        };
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

    #region Read doc content
    private async Task<IEnumerable<string>> GetFileContent(string contentType, byte[] bytes)
    {
        IEnumerable<string> results = new List<string>();

        if (contentType.IsEqualTo(MediaTypeNames.Text.Plain))
        {
            results = await ReadTxt(bytes);
        }
        else if (contentType.IsEqualTo(MediaTypeNames.Application.Pdf))
        {
            results = await ReadPdf(bytes);
        }
        
        return results;
    }

    private async Task<IEnumerable<string>> ReadTxt(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        reader.Close();
        stream.Close();

        var lines = TextChopper.Chop(content, new ChunkOption
        {
            Size = 1024,
            Conjunction = 12,
            SplitByWord = true,
        });
        return lines;
    }

    private async Task<IEnumerable<string>> ReadPdf(byte[] bytes)
    {
        return Enumerable.Empty<string>();
    }
    #endregion


    private bool SaveDocument(string collectionName, string vectorStoreProvider, Guid fileId, string fileName, byte[] bytes)
    {
        var fileStoreage = _services.GetRequiredService<IFileStorageService>();
        var data = BinaryData.FromBytes(bytes);
        var saved = fileStoreage.SaveKnowledgeBaseFile(collectionName, vectorStoreProvider, fileId, fileName, data);
        return saved;
    }

    private async Task<IEnumerable<string>> SaveToVectorDb(
        string collectionName, Guid fileId, string fileName, IEnumerable<string> contents, Dictionary<string, object>? payload = null)
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
            var saved = await vectorDb.Upsert(collectionName, dataId, vector, content, payload ?? new Dictionary<string, object>());

            if (!saved) continue;

            dataIds.Add(dataId.ToString());
        }

        return dataIds;
    }
    #endregion
}
