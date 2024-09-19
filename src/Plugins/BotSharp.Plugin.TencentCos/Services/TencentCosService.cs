using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Users;
using BotSharp.Plugin.TencentCos.Settings;
using System.Net.Mime;

namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService : IFileStorageService
{
    private readonly TencentCosClient _cosClient;
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
    private const string TEXT_TO_SPEECH_FOLDER = "speeches";
    private const string KNOWLEDGE_FOLDER = "knowledgebase";
    private const string KNOWLEDGE_DOC_FOLDER = "document";

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
        _fullBuketName = $"{settings.BucketName}-{settings.AppId}";
        _cosClient = cosClient;
    }
}
