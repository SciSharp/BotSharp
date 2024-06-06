using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Threading;

namespace BotSharp.Core.Files;

public partial class BotSharpFileService : IBotSharpFileService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger<BotSharpFileService> _logger;
    private readonly string _baseDir;
    private readonly IEnumerable<string> _allowedImageTypes = new List<string> { "image/png", "image/jpeg" };
    private readonly IEnumerable<string> _allowScreenShotTypes = new List<string> { ".pdf" };

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";
    private const string USERS_FOLDER = "users";
    private const string USER_AVATAR_FOLDER = "avatar";

    private const int MIN_OFFSET = 1;
    private const int MAX_OFFSET = 5;

    public BotSharpFileService(
        BotSharpDatabaseSettings dbSettings,
        IUserIdentity user,
        ILogger<BotSharpFileService> logger,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _user = user;
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

    private bool ExistDirectory(string? dir)
    {
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
    }
    #endregion
}
