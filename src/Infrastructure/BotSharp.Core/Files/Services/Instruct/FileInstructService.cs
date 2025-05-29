
using static System.Net.Mime.MediaTypeNames;

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

    #region Private methods
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

    private async Task<BinaryData> DownloadFile(InstructFileModel file)
    {
        var binary = BinaryData.Empty;

        try
        {
            if (!string.IsNullOrEmpty(file.FileUrl))
            {
                var http = _services.GetRequiredService<IHttpClientFactory>();
                using var client = http.CreateClient();
                var bytes = await client.GetByteArrayAsync(file.FileUrl);
                binary = BinaryData.FromBytes(bytes);
            }
            else if (!string.IsNullOrEmpty(file.FileData))
            {
                (_, binary) = FileUtility.GetFileInfoFromData(file.FileData);
            }

            return binary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error when downloading file {file.FileUrl}");
            return binary;
        }
    }

    private async Task<string?> GetAgentTemplate(string agentId, string? templateName)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(templateName))
        {
            return null;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);
        if (agent == null)
        {
            return null;
        }

        var instruction = agentService.RenderedTemplate(agent, templateName);
        return instruction;
    }

    private string BuildFileName(string? name, string? extension, string defaultName, string defaultExtension)
    {
        var fname = name.IfNullOrEmptyAs(defaultName);
        var fextension = extension.IfNullOrEmptyAs(defaultExtension);
        fextension = fextension.StartsWith(".") ? fextension.Substring(1) : fextension;
        return $"{name}.{fextension}";
    }
    #endregion
}
