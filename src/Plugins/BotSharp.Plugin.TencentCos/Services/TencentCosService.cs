using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Users;
using BotSharp.Plugin.TencentCos.Settings;
using System.Net.Mime;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService : IBotSharpFileService
{
    private readonly TencentCosSettings _settings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger<TencentCosService> _logger;
    private readonly string _fullBuketName;
    private readonly IEnumerable<string> _imageTypes = new List<string>
    {
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Jpeg
    };

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";
    private const string USER_FILE_FOLDER = "user";
    private const string SCREENSHOT_FILE_FOLDER = "screenshot";
    private const string BOT_FILE_FOLDER = "bot";
    private const string USERS_FOLDER = "users";
    private const string USER_AVATAR_FOLDER = "avatar";
    private const string SESSION_FOLDER = "sessions";

    private const int MIN_OFFSET = 1;
    private const int MAX_OFFSET = 5;

    private readonly TencentCosClient _cosClient;

    public TencentCosService(
        TencentCosSettings settings,
        IUserIdentity user,
        ILogger<TencentCosService> logger,
        IServiceProvider services,
        TencentCosClient cosClient)
    {
        _settings = settings;
        _user = user;
        _logger = logger;
        _services = services;
        _fullBuketName = $"{_settings.BucketName}-{_settings.AppId}";
        _cosClient = cosClient;
    }

    #region Private methods
    private bool ExistDirectory(string? dir)
    {
        return !string.IsNullOrEmpty(dir) && _cosClient.BucketClient.DirExists(dir);
    }
    #endregion
}
