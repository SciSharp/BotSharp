namespace BotSharp.Plugin.TencentCos.Services;

public partial class TencentCosService
{
    public async Task<string> ReadPdf(string? provider, string? model, string? modelId, string prompt, List<BotSharpFile> files)
    {
        var content = string.Empty;

        if (string.IsNullOrWhiteSpace(prompt) || files.IsNullOrEmpty())
        {
            return content;
        }

        var guid = Guid.NewGuid().ToString();
        var sessionDir = GetSessionDirectory(guid);

        try
        {
            var pdfFiles = await DownloadFiles(sessionDir, files);
            var images = await ConvertPdfToImages(pdfFiles);
            if (images.IsNullOrEmpty()) return content;

            var completion = CompletionProvider.GetChatCompletion(_services, provider: provider ?? "openai",
                model: model, modelId: modelId ?? "gpt-4", multiModal: true);
            var message = await completion.GetChatCompletions(new Agent()
            {
                Id = Guid.Empty.ToString(),
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, prompt)
                {
                    Files = images.Select(x => new BotSharpFile { FileStorageUrl = x }).ToList()
                }
            });

            content = message.Content;
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when analyzing pdf in file service: {ex.Message}\r\n{ex.InnerException}");
            return content;
        }
        finally
        {
            Directory.Delete(sessionDir, true);
        }
    }

    #region Private methods
    private string GetSessionDirectory(string id)
    {
        var dir = $"{SESSION_FOLDER}/{id}";
        return dir;
    }

    private async Task<IEnumerable<string>> DownloadFiles(string dir, List<BotSharpFile> files, string extension = "pdf")
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
                    (_, bytes) = GetFileInfoFromData(file.FileData);
                }

                if (!bytes.IsNullOrEmpty())
                {
                    var guid = Guid.NewGuid().ToString();
                    var fileDir = $"{dir}/{guid}";

                    var pdfDir = $"{fileDir}/{guid}.{extension}";


                    _cosClient.BucketClient.UploadBytes(pdfDir, bytes);
                    locs.Add(pdfDir);
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
        var converter = GetPdf2ImageConverter();
        if (converter == null || files.IsNullOrEmpty())
        {
            return images;
        }

        foreach (var file in files)
        {
            try
            {
                var segs = file.Split(Path.DirectorySeparatorChar);
                var dir = string.Join(Path.DirectorySeparatorChar, segs.SkipLast(1));
                var folder = Path.Combine(dir, "screenshots");
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
