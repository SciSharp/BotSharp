using BotSharp.Abstraction.Browsing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading;

namespace BotSharp.Core.Files;

public partial class BotSharpFileService
{
    public IEnumerable<MessageFileModel> GetChatImages(string conversationId, List<RoleDialogModel> conversations, int offset = 1)
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
            if (!ExistDirectory(dir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                var contentType = GetFileContentType(file);
                if (imageOnly && !_allowedImageTypes.Contains(contentType))
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

    public string GetMessageFile(string conversationId, string messageId, string fileName)
    {
        var dir = GetConversationFileDirectory(conversationId, messageId);
        if (!ExistDirectory(dir))
        {
            return string.Empty;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public async Task<bool> SaveMessageFiles(string conversationId, string messageId, List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return false;

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);
        if (!ExistDirectory(dir)) return false;

        var contextId = string.Empty;
        var web = _services.GetRequiredService<IWebBrowser>();
        var isNeedScreenShot = files.Any(x => _allowScreenShotTypes.Contains(Path.GetExtension(x.FileName)));
        var preFixPath = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);

        try
        {
            if (isNeedScreenShot)
            {
                var state = _services.GetRequiredService<IConversationStateService>();
                contextId = state.GetConversationId() ?? Guid.NewGuid().ToString();
                await web.LaunchBrowser(contextId, string.Empty);
            }

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

                if (isNeedScreenShot && _allowScreenShotTypes.Contains(fileType))
                {
                    var path = Path.Combine(preFixPath, fileName);
                    await web.GoToPage(contextId, path);
                    path = Path.Combine(preFixPath, $"{Guid.NewGuid()}.png");
                    await web.ScreenshotAsync(contextId, path);
                }
            }

            if (isNeedScreenShot)
            {
                await web.CloseBrowser(contextId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving conversation files: {ex.Message}");
            return false;
        }
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
                    Directory.Delete(newDir, true);
                }

                Directory.Move(prevDir, newDir);
            }
        }

        foreach (var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (!ExistDirectory(dir)) continue;

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
            if (!ExistDirectory(convDir)) continue;

            Directory.Delete(convDir, true);
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

    private string? FindConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId);
        return dir;
    }
    #endregion
}
