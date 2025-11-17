using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Templating;

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

    private async Task<string?> RenderAgentTemplate(string agentId, string? templateName, IDictionary<string, object>? data = null)
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

        var instruction = agentService.RenderTemplate(agent, templateName, data);
        return instruction;
    }

    private string RenderText(string text, IDictionary<string, object>? data = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var renderData = data != null
                        ? new Dictionary<string, object>(data)
                        : agentService.CollectRenderData(new Agent());
        return render.Render(text, renderData);
    }

    private string BuildFileName(string? name, string? extension, string defaultName, string defaultExtension)
    {
        var fname = name.IfNullOrEmptyAs(defaultName);
        var fextension = extension.IfNullOrEmptyAs(defaultExtension)!;
        fextension = fextension.StartsWith(".") ? fextension.Substring(1) : fextension;
        return $"{name}.{fextension}";
    }

    private IImageConverter? GetImageConverter(string? provider)
    {
        var converter = _services.GetServices<IImageConverter>().FirstOrDefault(x => x.Provider == (provider ?? "image-handler"));
        return converter;
    }
    #endregion
}
