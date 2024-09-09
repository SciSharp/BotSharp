using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;

namespace BotSharp.Plugin.KnowledgeBase.Services;

public partial class KnowledgeService
{
    public async Task<UploadKnowledgeResponse> UploadVectorKnowledge(string collectionName, IEnumerable<InputFileModel> files)
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
        var cleanCollectionName = collectionName.RemoveWhiteSpaces();
        var successFiles = new List<string>();
        var failedFiles = new List<string>();

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileData) || string.IsNullOrWhiteSpace(file.FileName))
            {
                continue;
            }

            var dataIds = new List<string>();

            try
            {
                // Chop text
                var (contentType, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Save file
                var fileId = Guid.NewGuid().ToString();
                var saved = fileStoreage.SaveKnowledgeFiles(cleanCollectionName, fileId, file.FileName, stream);
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
                await vectorDb.Upsert(collectionName, dataId, vector, content, new Dictionary<string, string>
                {
                    { "fileName", file.FileName },
                    { "fileId", fileId },
                    { "page", "0" }
                });

                dataIds.Add(dataId.ToString());
                successFiles.Add(file.FileName);
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
    #endregion
}
