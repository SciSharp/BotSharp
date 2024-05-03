using Microsoft.AspNetCore.Http;
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
    private const string SEPARATOR = ".";

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

    public IEnumerable<OutputFileModel> GetConversationFiles(string conversationId, string messageId)
    {
        var outputFiles = new List<OutputFileModel>();
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return outputFiles;
        }

        var context = _services.GetRequiredService<IHttpContextAccessor>();
        var request = context.HttpContext.Request;
        var host = $"{request.Scheme}{Uri.SchemeDelimiter}{request.Host.Value}";
        var dir = GetConversationFileDirectory(conversationId);

        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.Split('.');
            var fileMsgId = splits.First();

            if (fileMsgId != messageId) continue;

            var index = splits[1];
            var fileType = splits.Last();
            var model = new OutputFileModel()
            {
                FileUrl = $"{host}/conversation/{conversationId}/file/{messageId}/type/{fileType}/{index}",
                FileName = fileName,
                FileType = fileType
            };
            outputFiles.Add(model);
        }
        return outputFiles;
    }

    public string? GetMessageFile(string conversationId, string messageId, string fileType, int index)
    {
        var targetFile = $"{messageId}{SEPARATOR}{index}.{fileType}";
        var dir = GetConversationFileDirectory(conversationId);
        var files = Directory.GetFiles(dir);
        var found = files.FirstOrDefault(f =>
        {
            var fileName = f.Split(Path.DirectorySeparatorChar).Last();
            return fileName.IsEqualTo(targetFile);
        });

        return found;
    }

    public void SaveConversationFiles(List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return;

        var conversationId = files.First().ConversationId;
        var dir = GetConversationFileDirectory(conversationId);

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

            var fileName = $"{file.MessageId}{SEPARATOR}{i+1}{parsedFormat}";
            Thread.Sleep(100);
            File.WriteAllBytes(Path.Combine(dir, fileName), bytes);
        }
    }

    #region Private methods
    private string GetConversationFileDirectory(string conversationId)
    {
        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
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
    #endregion
}
