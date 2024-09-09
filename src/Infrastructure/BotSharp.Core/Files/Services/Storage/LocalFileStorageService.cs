using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class LocalFileStorageService : IFileStorageService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _baseDir;

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

    public LocalFileStorageService(
        BotSharpDatabaseSettings dbSettings,
        IUserIdentity user,
        ILogger<LocalFileStorageService> logger,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _user = user;
        _logger = logger;
        _services = services;
        _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository);
    }
}
