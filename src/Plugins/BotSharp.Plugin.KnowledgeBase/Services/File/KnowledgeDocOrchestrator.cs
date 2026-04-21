using System.Net.Http;
using BotSharp.Abstraction.VectorStorage.Filters;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public class KnowledgeDocOrchestrator : IKnowledgeDocOrchestrator
{
    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;
    private readonly ILogger<KnowledgeDocOrchestrator> _logger;

    public string Provider => "botsharp-knowledge-doc";

    public KnowledgeDocOrchestrator(
        IServiceProvider services,
        KnowledgeBaseSettings settings,
        ILogger<KnowledgeDocOrchestrator> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<UploadKnowledgeResponse> UploadDocumentsToKnowledge(
        string collectionName,
        IEnumerable<ExternalFileModel> files,
        KnowledgeFileHandleOptions? options = null)
    {
        var res = new UploadKnowledgeResponse
        {
            Success = [],
            Failed = files?.Select(x => x.FileName) ?? []
        };

        if (string.IsNullOrWhiteSpace(collectionName) || files.IsNullOrEmpty())
        {
            return res;
        }

        var knowledgebaseProvider = options?.DbProvider ?? _settings.VectorDb.Provider;
        var exist = await ExistCollection(collectionName, knowledgebaseProvider);
        if (!exist)
        {
            return res;
        }

        var knowledgeFiles = new List<FileKnowledgeWrapper>();
        var successFiles = new List<string>();
        var failedFiles = new List<string>();

        foreach (var file in files!)
        {
            if (string.IsNullOrWhiteSpace(file.FileData)
                && string.IsNullOrWhiteSpace(file.FileUrl))
            {
                continue;
            }

            try
            {
                // Get document info
                var (contentType, binary) = await GetFileInfo(file);
                var fileData = new FileBinaryDataModel
                {
                    FileName = file.FileName,
                    ContentType = contentType,
                    FileBinaryData = binary
                };
                var knowledges = await GetFileKnowledge(fileData, options);
                if (knowledges.IsNullOrEmpty())
                {
                    failedFiles.Add(file.FileName);
                    continue;
                }

                var fileId = Guid.NewGuid();
                var payload = new Dictionary<string, VectorPayloadValue>()
                {
                    { KnowledgePayloadName.DataSource, (VectorPayloadValue)VectorDataSource.File },
                    { KnowledgePayloadName.FileId, (VectorPayloadValue)fileId.ToString() },
                    { KnowledgePayloadName.FileName, (VectorPayloadValue)file.FileName },
                    { KnowledgePayloadName.FileSource, (VectorPayloadValue)file.FileSource }
                };

                if (!string.IsNullOrWhiteSpace(file.FileUrl))
                {
                    payload[KnowledgePayloadName.FileUrl] = (VectorPayloadValue)file.FileUrl;
                }

                foreach (var kg in knowledges)
                {
                    var kgPayload = new Dictionary<string, VectorPayloadValue>(kg.Payload ?? new Dictionary<string, VectorPayloadValue>());
                    foreach (var pair in payload)
                    {
                        kgPayload[pair.Key] = pair.Value;
                    }
                    kg.Payload = kgPayload;
                }

                knowledgeFiles.Add(new()
                {
                    FileId = fileId,
                    FileData = fileData,
                    FileSource = VectorDataSource.File,
                    FileKnowledges = knowledges
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when processing knowledge file ({file.FileName}).");
                failedFiles.Add(file.FileName);
                continue;
            }
        }

        var response = await HandleKnowledgeFiles(collectionName, knowledgebaseProvider, knowledgeFiles, saveFile: true);
        return new UploadKnowledgeResponse
        {
            Success = successFiles.Concat(response.Success).Distinct(),
            Failed = failedFiles.Concat(response.Failed).Distinct()
        };
    }

    public async Task<bool> DeleteKnowledgeDocument(string collectionName, Guid fileId, KnowledgeFileOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return false;
        }

        try
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var knowledgebaseProvider = options?.DbProvider ?? _settings.VectorDb.Provider;
            var vectorDb = GetVectorDb(knowledgebaseProvider);
            if (vectorDb == null)
            {
                return false;
            }

            // Get doc meta data
            var pageData = await db.GetKnowledgeBaseFileMeta(collectionName, knowledgebaseProvider, new KnowledgeFileFilter
            {
                Size = 1,
                FileIds = [fileId]
            });

            // Delete doc
            fileStorage.DeleteKnowledgeFile(collectionName, knowledgebaseProvider, fileId);

            var found = pageData?.Items?.FirstOrDefault();
            if (found != null && !found.VectorDataIds.IsNullOrEmpty())
            {
                var guids = found.VectorDataIds.Where(x => Guid.TryParse(x, out _)).Select(x => Guid.Parse(x)).ToList();
                await vectorDb.DeleteCollectionData(collectionName, guids);
            }

            await db.DeleteKnolwedgeBaseFileMeta(collectionName, knowledgebaseProvider, fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deleting knowledge document " +
                $"(Collection: {collectionName}, File id: {fileId})");
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
        var knowledgebaseProvider = filter?.DbProvider ?? _settings.VectorDb.Provider;

        // Get doc meta data
        var pagedData = await db.GetKnowledgeBaseFileMeta(collectionName, knowledgebaseProvider, filter);

        var files = pagedData.Items?.Select(x => new KnowledgeFileModel
        {
            FileId = x.FileId,
            FileName = x.FileName,
            FileSource = x.FileSource,
            FileExtension = Path.GetExtension(x.FileName),
            ContentType = x.ContentType,
            FileUrl = fileStorage.GetKnowledgeBaseFileUrl(collectionName, knowledgebaseProvider, x.FileId, x.FileName),
            RefData = x.RefData
        })?.ToList() ?? [];

        return new PagedItems<KnowledgeFileModel>
        {
            Items = files,
            Count = pagedData.Count
        };
    }

    public async Task<FileBinaryDataModel> GetKnowledgeDocumentBinaryData(string collectionName, Guid fileId, KnowledgeFileOptions? options = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = options?.DbProvider.IfNullOrEmptyAs(_settings.VectorDb.Provider) ?? _settings.VectorDb.Provider;

        // Get doc binary data
        var pageData = await db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, new KnowledgeFileFilter
        {
            Size = 1,
            FileIds = [fileId]
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
    private IVectorDb? GetVectorDb(string? dbProvider = null)
    {
        var provider = dbProvider.IfNullOrEmptyAs(_settings.VectorDb.Provider);
        var db = _services.GetServices<IVectorDb>().FirstOrDefault(x => x.Provider == provider);
        return db;
    }

    private async Task<ITextEmbedding> GetTextEmbedding(string collectionName)
    {
        return await KnowledgeSettingHelper.GetTextEmbeddingSetting(_services, collectionName);
    }

    private async Task<string> GetUserId()
    {
        var userIdentity = _services.GetRequiredService<IUserIdentity>();
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(userIdentity.Id);
        return user.Id;
    }

    private async Task<bool> ExistCollection(string collectionName, string? dbProvider)
    {
        var vectorDb = GetVectorDb(dbProvider);
        if (vectorDb == null)
        {
            return false;
        }

        var exist = await vectorDb.DoesCollectionExist(collectionName);
        if (exist) return true;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var configs = await db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
        {
            CollectionNames = [collectionName],
            VectorStorageProviders = [vectorDb.Provider]
        });

        return !configs.IsNullOrEmpty();
    }

    private async Task<(string, BinaryData)> GetFileInfo(ExternalFileModel file)
    {
        if (file == null)
        {
            return (string.Empty, BinaryData.Empty);
        }

        if (!string.IsNullOrWhiteSpace(file.FileUrl))
        {
            var http = _services.GetRequiredService<IHttpClientFactory>();
            var contentType = FileUtility.GetFileContentType(file.FileName);
            using var client = http.CreateClient();
            var bytes = await client.GetByteArrayAsync(file.FileUrl);
            return (contentType, BinaryData.FromBytes(bytes));
        }
        else if (!string.IsNullOrWhiteSpace(file.FileData))
        {
            var (contentType, binary) = FileUtility.GetFileInfoFromData(file.FileData);
            return (contentType, binary);
        }

        return (string.Empty, BinaryData.Empty);
    }

    private async Task<IEnumerable<FileKnowledgeModel>> GetFileKnowledge(FileBinaryDataModel file, KnowledgeFileHandleOptions? options)
    {
        var processor = _services.GetServices<IKnowledgeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(options?.Processor));
        if (processor == null)
        {
            return Enumerable.Empty<FileKnowledgeModel>();
        }

        var response = await processor.GetFileKnowledgeAsync(file, options: options);
        return response?.Success == true ? response.Knowledges ?? [] : [];
    }

    private bool SaveDocument(string collectionName, string knowledgebaseProvider, Guid fileId, string fileName, BinaryData binary)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var saved = fileStorage.SaveKnowledgeBaseFile(collectionName, knowledgebaseProvider, fileId, fileName, binary);
        return saved;
    }

    private async Task<IEnumerable<string>> SaveToKnowledgebase(string collectionName, string knowledgebaseProvider, IEnumerable<string> contents, Dictionary<string, VectorPayloadValue>? payload = null)
    {
        if (contents.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }

        var dataIds = new List<string>();
        var vectorDb = GetVectorDb(knowledgebaseProvider);
        var textEmbedding = await GetTextEmbedding(collectionName);

        for (int i = 0; i < contents.Count(); i++)
        {
            var content = contents.ElementAt(i);

            try
            {
                var vector = await textEmbedding.GetVectorAsync(content);
                var dataId = Guid.NewGuid();
                var saved = await vectorDb.Upsert(collectionName, dataId, vector, content, payload ?? []);

                if (!saved)
                {
                    continue;
                }

                dataIds.Add(dataId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when saving file knowledge to vector db collection {collectionName}. (Content: {content.SubstringMax(20)})");
            }
        }

        return dataIds;
    }

    private async Task<UploadKnowledgeResponse> HandleKnowledgeFiles(
        string collectionName,
        string knowledgebaseProvider,
        IEnumerable<FileKnowledgeWrapper> knowledgeFiles,
        bool saveFile = false)
    {
        if (knowledgeFiles.IsNullOrEmpty())
        {
            return new();
        }

        var successFiles = new List<string>();
        var failedFiles = new List<string>();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var userId = await GetUserId();
        foreach (var item in knowledgeFiles)
        {
            var file = item.FileData;

            // Save document
            if (saveFile)
            {
                var saved = SaveDocument(collectionName, knowledgebaseProvider, item.FileId, file.FileName, file.FileBinaryData);
                if (!saved)
                {
                    _logger.LogWarning($"Failed to save knowledge file: {file.FileName} to collection {collectionName}.");
                    failedFiles.Add(file.FileName);
                    continue;
                }
            }

            // Save to vector db
            var dataIds = new List<string>();
            foreach (var kg in item.FileKnowledges)
            {
                var ids = await SaveToKnowledgebase(collectionName, knowledgebaseProvider, kg.Contents, kg.Payload?.ToDictionary());
                dataIds.AddRange(ids);
            }

            if (!dataIds.IsNullOrEmpty())
            {
                await db.SaveKnolwedgeBaseFileMeta(new KnowledgeFileMetaData
                {
                    Collection = collectionName,
                    FileId = item.FileId,
                    FileName = file.FileName,
                    FileSource = item.FileSource ?? VectorDataSource.File,
                    ContentType = file.ContentType,
                    VectorStoreProvider = knowledgebaseProvider,
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

        return new UploadKnowledgeResponse
        {
            Success = successFiles,
            Failed = failedFiles
        };
    }
    #endregion
}
