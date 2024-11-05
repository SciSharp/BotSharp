using BotSharp.Abstraction.Files.Converters;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
{
    public async Task<IEnumerable<MessageFileModel>> GetMessageFileScreenshotsAsync(string conversationId, IEnumerable<string> messageIds)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || messageIds.IsNullOrEmpty())
        {
            return files;
        }

        var source = FileSourceType.User;
        var pathPrefix = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);

        foreach (var messageId in messageIds)
        {
            var dir = Path.Combine(pathPrefix, messageId, FileSourceType.User);
            if (!ExistDirectory(dir)) continue;

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var file = Directory.GetFiles(subDir).FirstOrDefault();
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
                    var fileExtension = Path.GetExtension(file).Substring(1);
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}",
                        FileDownloadUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}/download",
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

    public bool SaveMessageFiles(string conversationId, string messageId, string source, List<FileDataModel> files)
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

            DeleteDirectory(dir);
            Thread.Sleep(100);
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

    private async Task<IEnumerable<MessageFileModel>> GetScreenshots(string file, string parentDir, string messageId, string source)
    {
        var files = new List<MessageFileModel>();

        try
        {
            var contentType = FileUtility.GetFileContentType(file);
            var screenshotDir = Path.Combine(parentDir, SCREENSHOT_FILE_FOLDER);

            if (ExistDirectory(screenshotDir) && !Directory.GetFiles(screenshotDir).IsNullOrEmpty())
            {
                foreach (var screenshot in Directory.GetFiles(screenshotDir))
                {
                    var fileName = Path.GetFileNameWithoutExtension(screenshot);
                    var fileExtension = Path.GetExtension(screenshot).Substring(1);
                    var screenshotContentType = FileUtility.GetFileContentType(screenshot);
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        FileStorageUrl = screenshot,
                        ContentType = screenshotContentType,
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
                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileName = fileName,
                        FileExtension = fileExtension,
                        FileStorageUrl = image,
                        ContentType = screenshotContentType,
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
