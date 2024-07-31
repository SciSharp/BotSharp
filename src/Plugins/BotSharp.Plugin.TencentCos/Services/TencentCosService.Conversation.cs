using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Files.Enums;
using System.Net.Mime;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public async Task<IEnumerable<MessageFileModel>> GetChatFiles(string conversationId, string source,
        IEnumerable<RoleDialogModel> conversations, IEnumerable<string> contentTypes,
        bool includeScreenShot = false, int? offset = null)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || conversations.IsNullOrEmpty())
        {
            return files;
        }

        var messageIds = GetMessageIds(conversations, offset);
        var pathPrefix = $"{CONVERSATION_FOLDER}/{conversationId}/{FILE_FOLDER}";

        foreach (var messageId in messageIds)
        {
            var dir = $"{pathPrefix}/{messageId}/{source}";

            foreach (var subDir in _cosClient.BucketClient.GetDirectories(dir))
            {
                var file = _cosClient.BucketClient.GetDirFiles(subDir).FirstOrDefault();
                if (file == null) continue;

                var contentType = GetFileContentType(file);
                if (contentTypes?.Contains(contentType) != true) continue;

                var foundFiles = await GetMessageFiles(file, subDir, contentType, messageId, source, includeScreenShot);
                if (foundFiles.IsNullOrEmpty()) continue;

                files.AddRange(foundFiles);
            }
        }

        return files;
    }

    public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds,
        string source, bool imageOnly = false)
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
                    var contentType = GetFileContentType(file);
                    if (imageOnly && !_imageTypes.Contains(contentType))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileType = Path.GetExtension(file).Substring(1);
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileUrl = $"https://{_fullBuketName}.cos.{_settings.Region}.myqcloud.com/{file}",
                        FileStorageUrl = file,
                        FileName = fileName,
                        FileType = fileType,
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

    public bool SaveMessageFiles(string conversationId, string messageId, string source, List<BotSharpFile> files)
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
                var (_, bytes) = GetFileInfoFromData(file.FileData);

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

    private IEnumerable<string> GetMessageIds(IEnumerable<RoleDialogModel> conversations, int? offset = null)
    {
        if (conversations.IsNullOrEmpty()) return Enumerable.Empty<string>();

        if (offset <= 0)
        {
            offset = MIN_OFFSET;
        }
        else if (offset > MAX_OFFSET)
        {
            offset = MAX_OFFSET;
        }

        var messageIds = new List<string>();
        if (offset.HasValue)
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().TakeLast(offset.Value).ToList();
        }
        else
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().ToList();
        }

        return messageIds;
    }


    private async Task<IEnumerable<MessageFileModel>> GetMessageFiles(string file, string fileDir, string contentType,
        string messageId, string source, bool includeScreenShot)
    {
        var files = new List<MessageFileModel>();
        try
        {
            if (!_imageTypes.Contains(contentType) && includeScreenShot)
            {
                var screenShotDir = $"{fileDir}/{SCREENSHOT_FILE_FOLDER}/";

                var fileList = _cosClient.BucketClient.GetDirFiles(screenShotDir);

                if (!fileList.IsNullOrEmpty())
                {
                    foreach (var screenShot in fileList)
                    {
                        contentType = GetFileContentType(screenShot);
                        if (!_imageTypes.Contains(contentType)) continue;

                        var fileName = Path.GetFileNameWithoutExtension(screenShot);
                        var fileType = Path.GetExtension(file).Substring(1);
                        var model = new MessageFileModel()
                        {
                            MessageId = messageId,
                            FileName = fileName,
                            FileType = fileType,
                            FileStorageUrl = screenShot,
                            ContentType = contentType,
                            FileSource = source
                        };
                        files.Add(model);
                    }
                }
                else if (contentType == MediaTypeNames.Application.Pdf)
                {
                    var images = await ConvertPdfToImages(file, screenShotDir);
                    foreach (var image in images)
                    {
                        contentType = GetFileContentType(image);
                        var fileName = Path.GetFileNameWithoutExtension(image);
                        var fileType = Path.GetExtension(image).Substring(1);
                        var model = new MessageFileModel()
                        {
                            MessageId = messageId,
                            FileName = fileName,
                            FileType = fileType,
                            FileStorageUrl = image,
                            ContentType = contentType,
                            FileSource = source
                        };
                        files.Add(model);
                    }
                }
            }
            else
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var fileType = Path.GetExtension(file).Substring(1);
                var model = new MessageFileModel()
                {
                    MessageId = messageId,
                    FileName = fileName,
                    FileType = fileType,
                    FileStorageUrl = file,
                    ContentType = contentType,
                    FileSource = source
                };
                files.Add(model);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when getting message files {file} (messageId: {messageId}), Error: {ex.Message}\r\n{ex.InnerException}");
            return files;
        }
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
        var converters = _services.GetServices<IPdf2ImageConverter>();
        return converters.FirstOrDefault();
    }
    #endregion
}
