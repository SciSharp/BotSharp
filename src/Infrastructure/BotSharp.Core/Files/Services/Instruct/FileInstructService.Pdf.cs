using BotSharp.Abstraction.Files.Converters;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs;

namespace BotSharp.Core.Files.Services;

public partial class FileInstructService
{
    public async Task<string> ReadPdf(string text, List<InstructFileModel> files, InstructOptions? options = null)
    {
        var content = string.Empty;

        if (string.IsNullOrWhiteSpace(text) || files.IsNullOrEmpty())
        {
            return content;
        }

        var guid = Guid.NewGuid().ToString();
        var sessionDir = _fileStorage.BuildDirectory(SESSION_FOLDER, guid);
        DeleteIfExistDirectory(sessionDir, true);

        try
        {
            var pdfFiles = await DownloadFiles(sessionDir, files);
            var images = await ConvertPdfToImages(pdfFiles);
            if (images.IsNullOrEmpty()) return content;

            var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
            var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

            var completion = CompletionProvider.GetChatCompletion(_services, provider: options?.Provider ?? "openai",
                model: options?.Model ?? "gpt-4o", multiModal: true);
            var message = await completion.GetChatCompletions(new Agent()
            {
                Id = innerAgentId,
                Instruction = instruction
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, text)
                {
                    Files = images.Select(x => new BotSharpFile { FileStorageUrl = x }).ToList()
                }
            });

            var hooks = _services.GetServices<IInstructHook>();
            foreach (var hook in hooks)
            {
                if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != innerAgentId)
                {
                    continue;
                }

                await hook.OnResponseGenerated(new InstructResponseModel
                {
                    AgentId = innerAgentId,
                    Provider = completion.Provider,
                    Model = completion.Model,
                    TemplateName = options?.TemplateName,
                    UserMessage = text,
                    SystemInstruction = instruction,
                    CompletionText = message.Content
                });
            }

            return message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when analyzing pdf in file service: {ex.Message}\r\n{ex.InnerException}");
            return content;
        }
        finally
        {
            _fileStorage.DeleteDirectory(sessionDir);
        }
    }

    #region Private methods
    private async Task<IEnumerable<string>> DownloadFiles(string dir, List<InstructFileModel> files, string extension = "pdf")
    {
        if (string.IsNullOrWhiteSpace(dir) || files.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }

        var locs = new List<string>();
        foreach (var file in files)
        {
            try
            {
                var bytes = new byte[0];
                if (!string.IsNullOrEmpty(file.FileUrl))
                {
                    var http = _services.GetRequiredService<IHttpClientFactory>();
                    using var client = http.CreateClient();
                    bytes = await client.GetByteArrayAsync(file.FileUrl);
                }
                else if (!string.IsNullOrEmpty(file.FileData))
                {
                    (_, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
                }

                if (!bytes.IsNullOrEmpty())
                {
                    var guid = Guid.NewGuid().ToString();
                    var fileDir = _fileStorage.BuildDirectory(dir, guid);
                    DeleteIfExistDirectory(fileDir, true);

                    var outputDir = _fileStorage.BuildDirectory(fileDir, $"{guid}.{extension}");
                    _fileStorage.SaveFileBytesToPath(outputDir, bytes);
                    locs.Add(outputDir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when saving pdf file: {ex.Message}\r\n{ex.InnerException}");
                continue;
            }
        }
        return locs;
    }

    private async Task<IEnumerable<string>> ConvertPdfToImages(IEnumerable<string> files)
    {
        var images = new List<string>();
        var settings = _services.GetRequiredService<FileCoreSettings>();
        var converter = _services.GetServices<IPdf2ImageConverter>().FirstOrDefault(x => x.Provider == settings.Pdf2ImageConverter.Provider);
        if (converter == null || files.IsNullOrEmpty())
        {
            return images;
        }

        foreach (var file in files)
        {
            try
            {
                var dir = _fileStorage.GetParentDir(file);
                var folder = _fileStorage.BuildDirectory(dir, "screenshots");
                var urls = await converter.ConvertPdfToImages(file, folder);
                images.AddRange(urls);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when converting pdf file to images ({file}): {ex.Message}\r\n{ex.InnerException}");
                continue;
            }
        }
        return images;
    }
    #endregion
}
