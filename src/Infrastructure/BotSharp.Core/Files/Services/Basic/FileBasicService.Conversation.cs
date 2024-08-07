using BotSharp.Abstraction.Files.Converters;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileBasicService
{
    public async Task<IEnumerable<MessageFileModel>> GetChatFiles(string conversationId, string source,
        IEnumerable<RoleDialogModel> conversations, IEnumerable<string>? contentTypes = null,
        bool includeScreenShot = false, int? offset = null)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || conversations.IsNullOrEmpty())
        {
            return files;
        }

        var messageIds = GetMessageIds(conversations, offset);
        var pathPrefix = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);

        foreach (var messageId in messageIds)
        {
            var dir = Path.Combine(pathPrefix, messageId, source);
            if (!ExistDirectory(dir)) continue;

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var file = Directory.GetFiles(subDir).FirstOrDefault();
                if (file == null) continue;

                var contentType = FileUtility.GetFileContentType(file);
                if (!contentTypes.IsNullOrEmpty() && !contentTypes.Contains(contentType))
                {
                    continue;
                }

                var foundFiles = await GetMessageFiles(file, subDir, contentType, messageId, source, includeScreenShot);
                if (foundFiles.IsNullOrEmpty()) continue;

                files.AddRange(foundFiles);
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
            var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId, source);
            if (!ExistDirectory(dir))
            {
                continue;
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var index = subDir.Split(Path.DirectorySeparatorChar).Last();

                foreach (var file in Directory.GetFiles(subDir))
                {
                    var contentType = FileUtility.GetFileContentType(file);
                    if (!contentTypes.IsNullOrEmpty() && !contentTypes.Contains(contentType))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileType = Path.GetExtension(file).Substring(1);
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}",
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
        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId, source, index);
        if (!ExistDirectory(dir))
        {
            return string.Empty;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public IEnumerable<MessageFileModel> GetMessagesWithFile(string conversationId, IEnumerable<string> messageIds)
    {
        var foundMsgs = new List<MessageFileModel>();
        if (string.IsNullOrWhiteSpace(conversationId) || messageIds.IsNullOrEmpty()) return foundMsgs;

        foreach (var messageId in messageIds)
        {
            var prefix = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);
            var userDir = Path.Combine(prefix, FileSourceType.User);
            if (ExistDirectory(userDir))
            {
                foundMsgs.Add(new MessageFileModel { MessageId = messageId, FileSource = FileSourceType.User });
            }

            var botDir = Path.Combine(prefix, FileSourceType.Bot);
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
        if (!ExistDirectory(dir)) return false;

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
                var subDir = Path.Combine(dir, source, $"{i + 1}");
                if (!ExistDirectory(subDir))
                {
                    Directory.CreateDirectory(subDir);
                }

                using (var fs = new FileStream(Path.Combine(subDir, file.FileName), FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                    fs.Close();
                    Thread.Sleep(100);
                }
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
            var newDir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, newMessageId);

            if (ExistDirectory(prevDir))
            {
                if (ExistDirectory(newDir))
                {
                    DeleteDirectory(newDir);
                }

                Directory.Move(prevDir, newDir);
                Thread.Sleep(100);

                var botDir = Path.Combine(newDir, BOT_FILE_FOLDER);
                if (ExistDirectory(botDir))
                {
                    DeleteDirectory(botDir);
                }
            }
        }

        foreach (var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (!ExistDirectory(dir)) continue;

            Thread.Sleep(100);
            DeleteDirectory(dir);
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

            DeleteDirectory(convDir);
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

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);
        if (!Directory.Exists(dir) && createNewDir)
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    private string? GetConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId);
        return dir;
    }

    private IEnumerable<string> GetMessageIds(IEnumerable<RoleDialogModel> conversations, int? offset = null)
    {
        if (conversations.IsNullOrEmpty()) return Enumerable.Empty<string>();

        if (offset.HasValue && offset < 1)
        {
            offset = 1;
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
                var screenShotDir = Path.Combine(fileDir, SCREENSHOT_FILE_FOLDER);
                if (ExistDirectory(screenShotDir) && !Directory.GetFiles(screenShotDir).IsNullOrEmpty())
                {
                    foreach (var screenShot in Directory.GetFiles(screenShotDir))
                    {
                        contentType = FileUtility.GetFileContentType(screenShot);
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
                        contentType = FileUtility.GetFileContentType(image);
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
