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
            var provider = options?.Provider ?? "openai";
            var pdfFiles = await DownloadAndSaveFiles(sessionDir, files);

            var targetFiles = pdfFiles;
            if (provider != "google-ai")
            {
                targetFiles = await ConvertPdfToImages(pdfFiles);
            }

            if (targetFiles.IsNullOrEmpty())
            {
                return content;
            }

            var innerAgentId = options?.AgentId ?? Guid.Empty.ToString();
            var instruction = await GetAgentTemplate(innerAgentId, options?.TemplateName);

            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider,
                model: options?.Model ?? "gpt-4o", multiModal: true);
            var message = await completion.GetChatCompletions(new Agent()
            {
                Id = innerAgentId,
                Instruction = instruction
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, text)
                {
                    Files = targetFiles.Select(x => new BotSharpFile { FileStorageUrl = x }).ToList()
                }
            });

            await HookEmitter.Emit<IInstructHook>(_services, async hook =>
                await hook.OnResponseGenerated(new InstructResponseModel
                {
                    AgentId = innerAgentId,
                    Provider = completion.Provider,
                    Model = completion.Model,
                    TemplateName = options?.TemplateName,
                    UserMessage = text,
                    SystemInstruction = instruction,
                    CompletionText = message.Content
                }), innerAgentId);

            return message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when analyzing pdf in file service.");
            return content;
        }
        finally
        {
            _fileStorage.DeleteDirectory(sessionDir);
        }
    }

    #region Private methods
    private async Task<IEnumerable<string>> DownloadAndSaveFiles(string dir, List<InstructFileModel> files, string extension = "pdf")
    {
        if (string.IsNullOrWhiteSpace(dir) || files.IsNullOrEmpty())
        {
            return Enumerable.Empty<string>();
        }

        var downloadTasks = files.Select(x => DownloadFile(x));
        await Task.WhenAll(downloadTasks);

        var locs = new List<string>();
        for (int i = 0; i < files.Count; i++)
        {
            var binary = downloadTasks.ElementAt(i).Result;
            if (binary == null || binary.IsEmpty)
            {
                continue;
            }

            try
            {
                var guid = Guid.NewGuid().ToString();
                var fileDir = _fileStorage.BuildDirectory(dir, guid);
                DeleteIfExistDirectory(fileDir, createNew: true);

                var outputDir = _fileStorage.BuildDirectory(fileDir, $"{guid}.{extension}");
                _fileStorage.SaveFileBytesToPath(outputDir, binary);
                locs.Add(outputDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error when saving #{i + 1} {extension} file.");
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
                _logger.LogWarning(ex, $"Error when converting pdf file to images ({file}).");
                continue;
            }
        }
        return images;
    }
    #endregion
}
