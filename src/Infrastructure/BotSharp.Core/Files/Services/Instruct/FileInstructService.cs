namespace BotSharp.Core.Files.Services;

public partial class FileInstructService : IFileInstructService
{
    private readonly IFileStorageService _fileStorage;
    private readonly IServiceProvider _services;
    private readonly ILogger<FileInstructService> _logger;

    private const string SESSION_FOLDER = "sessions";

    public FileInstructService(
        IFileStorageService fileStorate,
        ILogger<FileInstructService> logger,
        IServiceProvider services)
    {
        _fileStorage = fileStorate;
        _logger = logger;
        _services = services;
    }

    private void DeleteIfExistDirectory(string? dir, bool createNew = false)
    {
        if (_fileStorage.ExistDirectory(dir))
        {
            _fileStorage.DeleteDirectory(dir);
        }
        else if (createNew)
        {
            _fileStorage.CreateDirectory(dir);
        }
    }
}
