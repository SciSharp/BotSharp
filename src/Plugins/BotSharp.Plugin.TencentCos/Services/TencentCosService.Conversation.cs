using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Files.Options;
using BotSharp.Abstraction.Files.Utilities;
using System.Net.Mime;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public async Task<IEnumerable<MessageFileModel>> GetMessageFileScreenshotsAsync(string conversationId, IEnumerable<string> messageIds, MessageFileScreenshotOptions options)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId)
            || messageIds.IsNullOrEmpty()
            || options.Sources.IsNullOrEmpty())
        {
            return files;
        }

        var baseDir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}";
        foreach (var messageId in messageIds)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                continue;
            }

            foreach (var source in options.Sources)
            {
                var dir = $"{baseDir}/{messageId}/{source}";
                foreach (var subDir in _cosClient.BucketClient.GetDirectories(dir))
                {
                    var file = _cosClient.BucketClient.GetDirFiles(subDir).FirstOrDefault();
                    if (file == null) continue;

                    var screenshots = await GetScreenshotsAsync(file, subDir, messageId, source, options);
                    if (screenshots.IsNullOrEmpty()) continue;

                    files.AddRange(screenshots);
                }
            }
        }

        return files;
    }

    public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, MessageFileOptions? options = null)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrWhiteSpace(conversationId) || messageIds.IsNullOrEmpty())
        {
            return files;
        }

        foreach (var messageId in messageIds)
        {
            var baseDir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{messageId}";
            if (!ExistDirectory(baseDir))
            {
                continue;
            }

            var sources = options?.Sources != null
                              ? options.Sources
                              : _cosClient.BucketClient.GetDirectories(baseDir).Select(x => x.Split("/", StringSplitOptions.RemoveEmptyEntries).Last());
            if (sources.IsNullOrEmpty())
            {
                continue;
            }

            foreach (var source in sources)
            {
                var dir = Path.Combine(baseDir, source);
                if (!ExistDirectory(dir))
                {
                    continue;
                }

                foreach (var subDir in _cosClient.BucketClient.GetDirectories(dir))
                {
                    var fileIndex = subDir.Split("/", StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

                    foreach (var file in _cosClient.BucketClient.GetDirFiles(subDir))
                    {
                        var contentType = FileUtility.GetFileContentType(file);
                        if (options?.ContentTypes != null && !options.ContentTypes.Contains(contentType))
                        {
                            continue;
                        }

                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var fileExtension = Path.GetExtension(file).Substring(1);
                        var model = new MessageFileModel()
                        {
                            MessageId = messageId,
                            FileUrl = BuilFileUrl(file),
                            FileDownloadUrl = BuilFileUrl(file),
                            FileStorageUrl = file,
                            FileName = fileName,
                            FileExtension = fileExtension,
                            ContentType = contentType,
                            FileSource = source,
                            FileIndex = fileIndex
                        };
                        files.Add(model);
                    }
                }
            }
        }

        return files;
    }

    

    public string GetMessageFile(string conversationId, string messageId, string source, string index, string fileName)
    {
        var dir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{source}/{index}";

        var fileList = _cosClient.BucketClient.GetDirFiles(dir);
        var found = fileList.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public bool SaveMessageFiles(string conversationId, string messageId, string source, List<FileDataModel> files)
    {
        if (files.IsNullOrEmpty()) return false;

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (string.IsNullOrEmpty(file.FileData))
            {
                continue;
            }

            try
            {
                var (_, binary) = FileUtility.GetFileInfoFromData(file.FileData);
                var subDir = $"{dir}/{source}/{i + 1}";
                _cosClient.BucketClient.UploadBytes($"{subDir}/{file.FileName}", binary.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,$"Error when saving message file {file.FileName} (conv id: {conversationId}).");
                continue;
            }
        }

        return true;
    }


    public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null)
    {
        if (string.IsNullOrEmpty(conversationId) || messageIds == null) return false;

        if (!string.IsNullOrEmpty(targetMessageId) && !string.IsNullOrEmpty(newMessageId))
        {
            var prevDir = GetConversationFileDirectory(conversationId, targetMessageId);
            var newDir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{newMessageId}/";

            if (ExistDirectory(prevDir))
            {
                if (ExistDirectory(newDir))
                {
                    _cosClient.BucketClient.DeleteDir(newDir);
                }

                _cosClient.BucketClient.MoveDir(prevDir, newDir);

                var botDir = $"{newDir}/{BOT_FILE_FOLDER}";
                if (ExistDirectory(botDir))
                {
                    _cosClient.BucketClient.DeleteDir(newDir);
                }
            }
        }

        foreach (var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (!ExistDirectory(dir)) continue;
            _cosClient.BucketClient.DeleteDir(dir);
        }

        return true;
    }

    public bool DeleteConversationFiles(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        foreach (var conversationId in conversationIds)
        {
            var convDir = GetConversationDirectory(conversationId);
            if (!ExistDirectory(convDir)) continue;

            _cosClient.BucketClient.DeleteDir(convDir);
        }
        return true;
    }

    #region Private methods
    private string GetConversationFileDirectory(string? conversationId, string? messageId, bool createNewDir = false)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return string.Empty;
        }

        return $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{messageId}";
    }

    private string? GetConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = $"{CONVERSATION_FOLDER}/{conversationId}";
        return dir;
    }

    private async Task<IEnumerable<string>> ConvertPdfToImagesAsync(string pdfLoc, string imageLoc, MessageFileScreenshotOptions options)
    {
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == options.ImageConvertProvider);
        if (converter == null)
        {
            return Enumerable.Empty<string>();
        }
        return await converter.ConvertPdfToImages(pdfLoc, imageLoc);
    }

    private string BuilFileUrl(string file)
    {
        return $"https://{_fullBuketName}.cos.{_settings.Region}.myqcloud.com/{file}";
    }

    private async Task<IEnumerable<MessageFileModel>> GetScreenshotsAsync(string file, string parentDir, string messageId, string source, MessageFileScreenshotOptions options)
    {
        var files = new List<MessageFileModel>();

        try
        {
            var contentType = FileUtility.GetFileContentType(file);
            var screenshotDir = $"{parentDir}/{SCREENSHOT_FILE_FOLDER}/";
            var screenshots = _cosClient.BucketClient.GetDirFiles(screenshotDir);
            if (!screenshots.IsNullOrEmpty())
            {
                foreach (var screenshot in screenshots)
                {
                    var screenshotContentType = FileUtility.GetFileContentType(screenshot);
                    var fileName = Path.GetFileNameWithoutExtension(screenshot);
                    var fileExtension = Path.GetExtension(screenshot).Substring(1);
                    var model = new MessageFileModel
                    {
                        MessageId = messageId,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        FileUrl = BuilFileUrl(screenshot),
                        FileStorageUrl = screenshot,
                        ContentType = contentType,
                        FileSource = source
                    };
                    files.Add(model);
                }
            }
            else if (contentType == MediaTypeNames.Application.Pdf)
            {
                var images = await ConvertPdfToImagesAsync(file, screenshotDir, options);
                foreach (var image in images)
                {
                    var fileName = Path.GetFileNameWithoutExtension(image);
                    var fileExtension = Path.GetExtension(image).Substring(1);
                    var screenshotContentType = FileUtility.GetFileContentType(image);
                    var model = new MessageFileModel
                    {
                        MessageId = messageId,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        FileUrl = BuilFileUrl(image),
                        FileStorageUrl = image,
                        ContentType = contentType,
                        FileSource = source
                    };
                    files.Add(model);
                }
            }
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when getting message file screenshots {file} (messageId: {messageId}).");
            return files;
        }
    }
    #endregion
}
