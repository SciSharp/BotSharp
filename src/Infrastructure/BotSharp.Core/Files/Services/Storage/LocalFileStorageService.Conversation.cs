using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Files.Options;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService
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

        var baseUrl = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);

        foreach (var messageId in messageIds)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                continue;
            }

            foreach (var source in options.Sources)
            {
                var dir = Path.Combine(baseUrl, messageId, source);
                if (!ExistDirectory(dir))
                {
                    continue;
                }

                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var file = Directory.EnumerateFiles(subDir).FirstOrDefault();
                    if (file == null)
                    {
                        continue;
                    }

                    var screenshots = await GetScreenshotsAsync(file, subDir, messageId, source, options);
                    if (screenshots.IsNullOrEmpty())
                    {
                        continue;
                    }

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
            if (string.IsNullOrWhiteSpace(messageId))
            {
                continue;
            }

            var baseDir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);
            if (!ExistDirectory(baseDir))
            {
                continue;
            }

            var sources = options?.Sources != null
                            ? options.Sources
                            : Directory.EnumerateDirectories(baseDir).Select(x => x.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last());
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

                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var fileIndex = subDir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

                    foreach (var file in Directory.EnumerateFiles(subDir))
                    {
                        var contentType = FileUtility.GetFileContentType(file);
                        if (options?.ContentTypes != null && !options.ContentTypes.Contains(contentType))
                        {
                            continue;
                        }

                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var fileExtension = Path.GetExtension(file).Substring(1);
                        var model = new MessageFileModel
                        {
                            MessageId = messageId,
                            FileUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{fileIndex}/{fileName}",
                            FileDownloadUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{fileIndex}/{fileName}/download",
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
        if (string.IsNullOrWhiteSpace(conversationId)
            || string.IsNullOrWhiteSpace(messageId)
            || string.IsNullOrWhiteSpace(source)
            || string.IsNullOrWhiteSpace(index))
        {
            return string.Empty;
        }

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId, source, index);
        if (!ExistDirectory(dir))
        {
            return string.Empty;
        }

        var found = Directory.EnumerateFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public bool SaveMessageFiles(string conversationId, string messageId, string source, List<FileDataModel> files)
    {
        if (string.IsNullOrWhiteSpace(conversationId)
            || string.IsNullOrWhiteSpace(messageId)
            || string.IsNullOrWhiteSpace(source)
            || files.IsNullOrEmpty())
        {
            return false;
        }

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);
        if (!ExistDirectory(dir))
        {
            return false;
        }

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
                var subDir = Path.Combine(dir, source, $"{i + 1}");
                if (!ExistDirectory(subDir))
                {
                    Directory.CreateDirectory(subDir);
                }

                using (var fs = new FileStream(Path.Combine(subDir, file.FileName), FileMode.Create))
                {
                    fs.Write(binary.ToArray(), 0, binary.Length);
                    fs.Flush(true);
                    fs.Close();
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error when saving message file {file.FileName} (conv id: {conversationId}).");
                continue;
            }
        }

        return true;
    }


    public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null)
    {
        if (string.IsNullOrEmpty(conversationId) || messageIds == null)
        {
            return false;
        }

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
            if (!ExistDirectory(dir))
            {
                continue;
            }

            DeleteDirectory(dir);
            Thread.Sleep(100);
        }

        return true;
    }

    public bool DeleteConversationFiles(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var conversationId in conversationIds)
        {
            var convDir = GetConversationDirectory(conversationId);
            if (!ExistDirectory(convDir))
            {
                continue;
            }

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

    private async Task<IEnumerable<string>> ConvertPdfToImagesAsync(string pdfLoc, string imageLoc, MessageFileScreenshotOptions options)
    {
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == options.ImageConvertProvider);
        if (converter == null)
        {
            return Enumerable.Empty<string>();
        }

        return await converter.ConvertPdfToImages(pdfLoc, imageLoc);
    }

    private async Task<IEnumerable<MessageFileModel>> GetScreenshotsAsync(string file, string parentDir, string messageId, string source, MessageFileScreenshotOptions options)
    {
        var files = new List<MessageFileModel>();

        try
        {
            var contentType = FileUtility.GetFileContentType(file);
            var screenshotDir = Path.Combine(parentDir, SCREENSHOT_FILE_FOLDER);

            if (ExistDirectory(screenshotDir))
            {
                foreach (var screenshot in Directory.EnumerateFiles(screenshotDir))
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
                var images = await ConvertPdfToImagesAsync(file, screenshotDir, options);
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
            _logger.LogWarning(ex, $"Error when getting message file screenshots {file} (messageId: {messageId})");
            return files;
        }
    }
    #endregion
}
