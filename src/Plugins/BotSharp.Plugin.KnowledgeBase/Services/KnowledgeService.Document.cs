using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Proccessors;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Knowledges.Filters;
using BotSharp.Abstraction.Knowledges.Helpers;
using BotSharp.Abstraction.Knowledges.Options;
using BotSharp.Abstraction.Knowledges.Responses;
using BotSharp.Abstraction.VectorStorage.Enums;
using System.Net.Http;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<UploadKnowledgeResponse> UploadDocumentsToKnowledge(
        string collectionName,
        IEnumerable<ExternalFileModel> files,
        KnowledgeDocOptions? options = null)
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

        var exist = await ExistVectorCollection(collectionName);
        if (!exist)
        {
            return res;
        }

        var fileStoreage = _services.GetRequiredService<IFileStorageService>();
        var vectorStoreProvider = _settings.VectorDb.Provider;
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

        var response = await HandleKnowledgeFiles(collectionName, vectorStoreProvider, knowledgeFiles, saveFile: true);
        return new UploadKnowledgeResponse
        {
            Success = successFiles.Concat(response.Success).Distinct(),
            Failed = failedFiles.Concat(response.Failed).Distinct()
        };
    }


    public async Task<bool> ImportDocumentContentToKnowledge(string collectionName, string fileName, string fileSource,
        IEnumerable<string> contents, DocMetaRefData? refData = null, Dictionary<string, VectorPayloadValue>? payload = null)
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

            var innerPayload = new Dictionary<string, VectorPayloadValue>(payload ?? []);
            innerPayload[KnowledgePayloadName.DataSource] = (VectorPayloadValue)VectorDataSource.File;
            innerPayload[KnowledgePayloadName.FileId] = (VectorPayloadValue)fileId.ToString();
            innerPayload[KnowledgePayloadName.FileName] = (VectorPayloadValue)fileName;
            innerPayload[KnowledgePayloadName.FileSource] = (VectorPayloadValue)fileSource;

            if (!string.IsNullOrWhiteSpace(refData?.Url))
            {
                innerPayload[KnowledgePayloadName.FileUrl] = (VectorPayloadValue)refData.Url;
            }

            var kgFile = new FileKnowledgeWrapper
            {
                FileId = fileId,
                FileSource = fileSource,
                FileData = new()
                {
                    FileName = fileName,
                    ContentType = contentType,
                    FileBinaryData = BinaryData.Empty
                },
                FileKnowledges = new List<FileKnowledgeModel>
                {
                    new()
                    {
                        Contents = contents,
                        Payload = innerPayload
                    }
                }
            };
            await HandleKnowledgeFiles(collectionName, vectorStoreProvider, [kgFile], saveFile: false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when importing doc content to knowledgebase ({collectionName}-{fileName})");
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
            var pageData = await db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, new KnowledgeFileFilter
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
        var vectorStoreProvider = _settings.VectorDb.Provider;

        // Get doc meta data
        var pagedData = await db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, filter);

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
        var pageData = await db.GetKnowledgeBaseFileMeta(collectionName, vectorStoreProvider, new KnowledgeFileFilter
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

    #region Read doc content
    private async Task<IEnumerable<FileKnowledgeModel>> GetFileKnowledge(FileBinaryDataModel file, KnowledgeDocOptions? options)
    {
        var processor = _services.GetServices<IFileProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(options?.Processor));
        if (processor == null)
        {
            return Enumerable.Empty<FileKnowledgeModel>();
        }

        var response = await processor.GetFileKnowledgeAsync(file, options: new() { });
        return response?.Knowledges ?? [];
    }

    private async Task<IEnumerable<string>> ReadTxt(BinaryData binary, ChunkOption option)
    {
        using var stream = binary.ToStream();
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        reader.Close();
        stream.Close();

        var lines = TextChopper.Chop(content, option);
        return lines;
    }
    #endregion


    private bool SaveDocument(string collectionName, string vectorStoreProvider, Guid fileId, string fileName, BinaryData binary)
    {
        var fileStoreage = _services.GetRequiredService<IFileStorageService>();
        var saved = fileStoreage.SaveKnowledgeBaseFile(collectionName, vectorStoreProvider, fileId, fileName, binary);
        return saved;
    }

    private async Task<IEnumerable<string>> SaveToVectorDb(string collectionName, IEnumerable<string> contents, Dictionary<string, VectorPayloadValue>? payload = null)
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
        string vectorStore,
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
                var saved = SaveDocument(collectionName, vectorStore, item.FileId, file.FileName, file.FileBinaryData);
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
                var ids = await SaveToVectorDb(collectionName, kg.Contents, kg.Payload?.ToDictionary());
                dataIds.AddRange(ids);
            }

            if (!dataIds.IsNullOrEmpty())
            {
                db.SaveKnolwedgeBaseFileMeta(new KnowledgeDocMetaData
                {
                    Collection = collectionName,
                    FileId = item.FileId,
                    FileName = file.FileName,
                    FileSource = item.FileSource ?? VectorDataSource.File,
                    ContentType = file.ContentType,
                    VectorStoreProvider = vectorStore,
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
