using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using System.Threading;

namespace BotSharp.Core.Files;

public class BotSharpFileService : IBotSharpFileService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly ILogger<BotSharpFileService> _logger;
    private readonly string _baseDir;
    private readonly IEnumerable<string> _allowedTypes = new List<string> { "image/png", "image/jpeg" };

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";
    private const int MIN_OFFSET = 1;
    private const int MAX_OFFSET = 5;

    public BotSharpFileService(
        BotSharpDatabaseSettings dbSettings,
        ILogger<BotSharpFileService> logger,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _logger = logger;
        _services = services;
        _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository);
    }

    public string GetDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, CONVERSATION_FOLDER, conversationId, "attachments");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public IEnumerable<MessageFileModel> GetChatImages(string conversationId, List<RoleDialogModel> conversations, int offset = 2)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || conversations.IsNullOrEmpty())
        {
            return files;
        }

        if (offset <= 0)
        {
            offset = MIN_OFFSET;
        }
        else if (offset > MAX_OFFSET)
        {
            offset = MAX_OFFSET;
        }

        var messageIds = conversations.Select(x => x.MessageId).Distinct().TakeLast(offset).ToList();
        files = GetMessageFiles(conversationId, messageIds, imageOnly: true).ToList();
        return files;
    }

    public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, bool imageOnly = false)
    {
        var files = new List<MessageFileModel>();
        if (messageIds.IsNullOrEmpty()) return files;

        foreach (var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (string.IsNullOrEmpty(dir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                var contentType = GetFileContentType(file);
                if (imageOnly && !_allowedTypes.Contains(contentType))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file);
                var fileType = extension.Substring(1);

                var model = new MessageFileModel()
                {
                    MessageId = messageId,
                    FileUrl = $"/conversation/{conversationId}/message/{messageId}/file/{fileName}",
                    FileStorageUrl = file,
                    FileName = fileName,
                    FileType = fileType,
                    ContentType = contentType
                };
                files.Add(model);
            }
        }
        
        return files;
    }

    public string? GetMessageFile(string conversationId, string messageId, string fileName)
    {
        var dir = GetConversationFileDirectory(conversationId, messageId);
        if (string.IsNullOrEmpty(dir))
        {
            return null;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public void SaveMessageFiles(string conversationId, string messageId, List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return;

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);
        if (string.IsNullOrEmpty(dir)) return;

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (string.IsNullOrEmpty(file.FileData))
                {
                    continue;
                }

                var (_, bytes) = GetFileInfoFromData(file.FileData);
                var fileType = Path.GetExtension(file.FileName);
                var fileName = $"{i + 1}{fileType}";
                Thread.Sleep(100);
                File.WriteAllBytes(Path.Combine(dir, fileName), bytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when saving conversation files: {ex.Message}");
        }
    }

    public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null)
    {
        if (string.IsNullOrEmpty(conversationId) ||  messageIds == null) return false;

        if (!string.IsNullOrEmpty(targetMessageId) && !string.IsNullOrEmpty(newMessageId))
        {
            var prevDir = GetConversationFileDirectory(conversationId, targetMessageId);
            var newDir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, newMessageId);

            if (Directory.Exists(prevDir))
            {
                if (Directory.Exists(newDir))
                {
                    Directory.Delete(newDir, true);
                }

                Directory.Move(prevDir, newDir);
            }
        }

        foreach ( var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (string.IsNullOrEmpty(dir)) continue;

            Thread.Sleep(100);
            Directory.Delete(dir, true);
        }

        return true;
    }

    public bool DeleteConversationFiles(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        foreach (var conversationId in conversationIds)
        {
            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) continue;

            Directory.Delete(convDir, true);
        }
        return true;
    }

    public (string, byte[]) GetFileInfoFromData(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return (string.Empty, new byte[0]);
        }

        var typeStartIdx = data.IndexOf(':');
        var typeEndIdx = data.IndexOf(';');
        var contentType = data.Substring(typeStartIdx + 1, typeEndIdx - typeStartIdx - 1);

        var base64startIdx = data.IndexOf(',');
        var base64Str = data.Substring(base64startIdx + 1);

        return (contentType, Convert.FromBase64String(base64Str));
    }

    #region Private methods
    private string GetConversationFileDirectory(string? conversationId, string? messageId, bool createNewDir = false)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return string.Empty;
        }

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);
        if (!Directory.Exists(dir))
        {
            if (createNewDir)
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                return string.Empty;
            }
        }
        return dir;
    }

    private string? FindConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId);
        if (!Directory.Exists(dir)) return null;

        return dir;
    }

    private string GetFileContentType(string filePath)
    {
        string contentType;
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out contentType))
        {
            contentType = string.Empty;
        }

        return contentType;
    }
    #endregion
}
