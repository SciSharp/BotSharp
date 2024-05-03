using System.IO;
using System.IO.Enumeration;
using System.Threading;

namespace BotSharp.Core.Conversations.Services;

public class ConversationAttachmentService : IConversationAttachmentService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly string _baseDir;

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";

    public ConversationAttachmentService(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
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

    public string GetConversationFileDirectory(string conversationId)
    {
        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public void SaveConversationFiles(List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return;

        var converationId = files.First().ConversationId;
        var dir = GetConversationFileDirectory(converationId);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (string.IsNullOrEmpty(file.ConversationId)
                || string.IsNullOrEmpty(file.MessageId)
                || string.IsNullOrEmpty(file.FileData))
            {
                continue;
            }

            var fileType = GetFileType(file.FileData);
            var bytes = GetFileBytes(file.FileData);
            var parsedFormat = ParseFileFormat(fileType);
            if (string.IsNullOrEmpty(parsedFormat))
            {
                continue;
            }

            var fileName = $"{file.MessageId}-{i+1}{parsedFormat}";
            Thread.Sleep(100);
            File.WriteAllBytes(Path.Combine(dir, fileName), bytes);
        }
    }

    private string GetFileType(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        var startIdx = data.IndexOf(':');
        var endIdx = data.IndexOf(';');
        var fileType = data.Substring(startIdx + 1, endIdx - startIdx - 1);
        return fileType;
    }

    private byte[] GetFileBytes(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return new byte[0];
        }

        var startIdx = data.IndexOf(',');
        var base64Str = data.Substring(startIdx + 1);
        return Convert.FromBase64String(base64Str);
    }

    private string ParseFileFormat(string type)
    {
        var parsed = string.Empty;
        switch (type)
        {
            case "image/png":
                parsed = ".png";
                break;
            case "image/jpeg":
            case "image/jpg":
                parsed = ".jpeg";
                break;
            case "application/pdf":
                parsed = ".pdf";
                break;
            case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                parsed = ".xlsx";
                break;
            case "text/plain":
                parsed = ".txt";
                break;
        }
        return parsed;
    }
}
