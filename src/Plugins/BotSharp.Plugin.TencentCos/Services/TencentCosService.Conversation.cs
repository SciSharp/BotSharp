using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.Files.Utilities;
using System.Net.Mime;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public async Task<IEnumerable<MessageFileModel>> GetMessageFileScreenshotsAsync(string conversationId, IEnumerable<string> messageIds)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || messageIds.IsNullOrEmpty())
        {
            return files;
        }

        var source = FileSourceType.User;
        var pathPrefix = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}";
        foreach (var messageId in messageIds)
        {
            var dir = $"{pathPrefix}/{messageId}/{source}";
            foreach (var subDir in _cosClient.BucketClient.GetDirectories(dir))
            {
                var file = _cosClient.BucketClient.GetDirFiles(subDir).FirstOrDefault();
                if (file == null) continue;

                var screenshots = await GetScreenshots(file, subDir, messageId, source);
                if (screenshots.IsNullOrEmpty()) continue;

                files.AddRange(screenshots);
            }
        }

        return files;
    }

    public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds,
        string source, IEnumerable<string>? contentTypes = null)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrWhiteSpace(conversationId) || messageIds.IsNullOrEmpty()) return files;

        foreach (var messageId in messageIds)
        {
            var dir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{messageId}/{source}";
            if (!ExistDirectory(dir))
            {
                continue;
            }

            foreach (var subDir in _cosClient.BucketClient.GetDirectories(dir))
            {
                foreach (var file in _cosClient.BucketClient.GetDirFiles(subDir))
                {
                    var contentType = FileUtility.GetFileContentType(file);
                    if (!contentTypes.IsNullOrEmpty() && !contentTypes.Contains(contentType))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileExtension = Path.GetExtension(file).Substring(1);
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileUrl = BuilFileUrl(file),
                        FileStorageUrl = file,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        ContentType = contentType,
                        FileSource = source
                    };
                    files.Add(model);
                }
            }
        }

        return files;
    }

    

    public string GetMessageFile(string conversationId, string messageId, string source, string index, string fileName)
    {
        var dir = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{source}/{index}/";

        var fileList = _cosClient.BucketClient.GetDirFiles(dir);
        var found = fileList.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public IEnumerable<MessageFileModel> GetMessagesWithFile(string conversationId, IEnumerable<string> messageIds)
    {
        var foundMsgs = new List<MessageFileModel>();
        if (string.IsNullOrWhiteSpace(conversationId) || messageIds.IsNullOrEmpty()) return foundMsgs;

        foreach (var messageId in messageIds)
        {
            var prefix = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}/{messageId}";
            var userDir = $"{prefix}/{FileSourceType.User}/";
            if (ExistDirectory(userDir))
            {
                foundMsgs.Add(new MessageFileModel { MessageId = messageId, FileSource = FileSourceType.User });
            }

            var botDir = $"{prefix}/{FileSourceType.Bot}";
            if (ExistDirectory(botDir))
            {
                foundMsgs.Add(new MessageFileModel { MessageId = messageId, FileSource = FileSourceType.Bot });
            }
        }

        return foundMsgs;
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
                var (_, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
                var subDir = $"{dir}/{source}/{i + 1}";
                _cosClient.BucketClient.UploadBytes($"{subDir}/{file.FileName}", bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when saving message file {file.FileName}: {ex.Message}\r\n{ex.InnerException}");
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

    private IEnumerable<string> GetMessageIds(IEnumerable<RoleDialogModel> dialogs, int? offset = null)
    {
        if (dialogs.IsNullOrEmpty()) return Enumerable.Empty<string>();

        if (offset.HasValue && offset < 1)
        {
            offset = 1;
        }

        var messageIds = new List<string>();
        if (offset.HasValue)
        {
            messageIds = dialogs.Select(x => x.MessageId).Distinct().TakeLast(offset.Value).ToList();
        }
        else
        {
            messageIds = dialogs.Select(x => x.MessageId).Distinct().ToList();
        }

        return messageIds;
    }


    private async Task<IEnumerable<string>> ConvertPdfToImages(string pdfLoc, string imageLoc)
    {
        var converters = _services.GetServices<IPdf2ImageConverter>();
        if (converters.IsNullOrEmpty()) return Enumerable.Empty<string>();

        var converter = GetPdf2ImageConverter();
        if (converter == null)
        {
            return Enumerable.Empty<string>();
        }
        return await converter.ConvertPdfToImages(pdfLoc, imageLoc);
    }

    private IPdf2ImageConverter? GetPdf2ImageConverter()
    {
        var settings = _services.GetRequiredService<FileCoreSettings>();
        var converter = _services.GetServices<IPdf2ImageConverter>().FirstOrDefault(x => x.Provider == settings.Pdf2ImageConverter.Provider);
        return converter;
    }

    private string BuilFileUrl(string file)
    {
        return $"https://{_fullBuketName}.cos.{_settings.Region}.myqcloud.com/{file}";
    }

    private async Task<IEnumerable<MessageFileModel>> GetScreenshots(string file, string parentDir, string messageId, string source)
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
                var images = await ConvertPdfToImages(file, screenshotDir);
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
            _logger.LogWarning($"Error when getting message file screenshots {file} (messageId: {messageId}), Error: {ex.Message}\r\n{ex.InnerException}");
            return files;
        }
    }
    #endregion
}
