namespace BotSharp.Core.Files.Services;

public partial class FileInstructService : IFileInstructService
{
    private readonly IFileBasicService _fileBasic;
    private readonly IServiceProvider _services;
    private readonly ILogger<FileInstructService> _logger;

    private const string SESSION_FOLDER = "sessions";

    public FileInstructService(
        IFileBasicService fileBasic,
        ILogger<FileInstructService> logger,
        IServiceProvider services)
    {
        _fileBasic = fileBasic;
        _logger = logger;
        _services = services;
    }

    private void DeleteIfExistDirectory(string? dir)
    {
        if (_fileBasic.ExistDirectory(dir))
        {
            _fileBasic.DeleteDirectory(dir);
        }
        else
        {
            _fileBasic.CreateDirectory(dir);
        }
    }
}
